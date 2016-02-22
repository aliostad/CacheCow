using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using CacheCow.Common;
using CacheCow.Common.Helpers;
using CacheCow.Common.Http;
using CacheCow.Server.ETagGeneration;
using CacheCow.Server.RoutePatternPolicy;

namespace CacheCow.Server
{
	/// <summary>
	/// Represents a message handler that implements caching and supports
	/// (loosely based on Glenn Block's ETagHandler)
	/// * Resource retrieval by ETag
	/// * Resource retrieval by LastModified
	/// * If-Match and If-None-Match for GET operations
	/// * If-Modified-Since and If-Unmodified-Since for GET operations
	/// * If-Unmodified-Since and If-Match for PUT operations
	/// * Will add ETag, LastModified and Vary headers in the response
	/// * Allows caching to be turned off based on individual message
	/// * Currently does not support If-Range headers
	/// </summary>
	public class CachingHandler : DelegatingHandler, ICachingHandler
	{

        // NOTE: !!!
        // This class is heavily functional. The reason is ease of unit testing each 
        // individual function/ 

		protected readonly IEntityTagStore _entityTagStore;
		private readonly string[] _varyByHeaders;
		private object _padLock = new object();
	    private HttpConfiguration _configuration;
	    private IRoutePatternProvider _routePatternProvider;

	    /// <summary>
		/// A Chain of responsibility of rules for handling various scenarios. 
		/// List is ordered. First one to return a non-null task will break the chain and 
		/// method will return
		/// </summary>
		protected IDictionary<string, Func<HttpRequestMessage, Task<HttpResponseMessage>>> RequestInterceptionRules { get; set; }
        
		public bool AddLastModifiedHeader { get; set; }

		public bool AddVaryHeader { get; set; }

        static CachingHandler()
        {
            IgnoreExceptionPolicy = (e) => { };
        }

        public CachingHandler(HttpConfiguration configuration, params string[] varyByHeader)
			: this(configuration, new InMemoryEntityTagStore(), varyByHeader)
		{

		}

	    public CachingHandler(HttpConfiguration configuration, IEntityTagStore entityTagStore, params string[] varyByHeaders)
		{
	        _configuration = configuration;
	        AddLastModifiedHeader = true;
			AddVaryHeader = true;
			_varyByHeaders = varyByHeaders;
			_entityTagStore = entityTagStore;
	        ETagValueGenerator = new DefaultETagGenerator().Generate;


	        UriTrimmer = (uri) => uri.PathAndQuery;
            
            _routePatternProvider = new ConventionalRoutePatternProvider(configuration);


            // infinite - Never refresh
	        CacheRefreshPolicyProvider = (message, httpConfiguration) => TimeSpan.MaxValue;

            // items by default get cached but must be revalidated
			CacheControlHeaderProvider = (request, cfg) => new CacheControlHeaderValue()
			{
				Private = true,
				MustRevalidate = true,
				NoTransform = true,
				MaxAge = TimeSpan.Zero
			};
		}

		/// <summary>
		/// A function which receives URL of the resource and generates a unique value for ETag
		/// It also receives request headers.
		/// Default value is a function that generates a guid and URL is ignored and
		/// it generates a weak ETag if no varyByHeaders is passed in
		/// </summary>
		public Func<string, IEnumerable<KeyValuePair<string, IEnumerable<string>>>,
			EntityTagHeaderValue> ETagValueGenerator { get; set; }

		/// <summary>
		/// This is a function that decides whether caching for a particular request
		/// is supported.
		/// Function can return null to negate any caching. In this case, responses will not be cached
		/// and ETag header will not be sent.
		/// Alternatively it can return a CacheControlHeaderValue which controls cache lifetime on the client.
		/// By default value is set so that all requests are cachable with immediate expiry.
		/// </summary>
		public Func<HttpRequestMessage, HttpConfiguration, CacheControlHeaderValue> CacheControlHeaderProvider { get; set; }

        /// <summary>
        /// This is a function responsible for controlling server's cache expiry
        /// By default, there is no expiry in the cache items as any change 
        /// to the resource must be done via the HTTP API (using POST, PUT, DELETE).
        /// But in some cases (usually adding Web API on top of legacy code), data is changed 
        /// in the database (e.g. configuration data) but server would not know.
        /// In these cases a cache expiry is useful. In this case, CachingHandler uses the 
        /// LastModified to calculate whether cache key must be expired.
        /// </summary>
        public Func<HttpRequestMessage, HttpConfiguration, TimeSpan> CacheRefreshPolicyProvider { get; set; } 

		/// <summary>
		/// This is a function to allow the clients to invalidate the cache
		/// for related URLs.
		/// Current request is passed and a list of URLs
		/// is retrieved and cache is invalidated for those URLs.
		/// </summary>

        [Obsolete("This is obsolte and is not hooked anymore. Please use IRoutePatternProvider interface.")]
		public Func<HttpRequestMessage, IEnumerable<string>> LinkedRoutePatternProvider { get; set; }

		/// <summary>
		/// A function that gets the Uri (normally request) and extracts important bits
		/// for keys. By default it will return Uri.PathAndQuery
		/// </summary>
		public Func<Uri, string> UriTrimmer { get; set; }

        /// <summary>
        /// Ignores all exceptions. Set it to ExceptionHandler
        /// </summary>
        public static Action<Exception> IgnoreExceptionPolicy { get; private set; }

        /// <summary>
        /// Provides route pattern and linked route pattern
        /// </summary>
	    public IRoutePatternProvider RoutePatternProvider {
            get{ return _routePatternProvider;  }
            set { _routePatternProvider = value; } 
        }

        public CacheKey GenerateCacheKey(HttpRequestMessage request)
        {            
            return new CacheKey(UriTrimmer(request.RequestUri), 
                request.Headers.ExtractHeadersValues(_varyByHeaders)
                .SelectMany(h => h.Value),
                _routePatternProvider.GetRoutePattern(request));
        }

	    public async Task InvalidateResourceAsync(HttpRequestMessage request)
	    {

            // remove resource
	        await _entityTagStore.RemoveResourceAsync(request.RequestUri.AbsolutePath);

            // remove by pattern
	        await _entityTagStore.RemoveAllByRoutePatternAsync(_routePatternProvider.GetRoutePattern(request));

            // remove all linked patterns - only need to do this once per uri
	        var routePatterns = _routePatternProvider.GetLinkedRoutePatterns(request);
	        foreach (var routePattern in routePatterns)
	        {
	            await _entityTagStore.RemoveAllByRoutePatternAsync(routePattern);
	        }
	    }

		protected async Task ExecuteCacheInvalidationRules(CacheKey cacheKey,
			HttpRequestMessage request,
			HttpResponseMessage response)
		{
		    var chain = new[]
		    {
		        InvalidateCache(cacheKey, request, response), // general invalidation
		        PostInvalidationRule(cacheKey, request, response)
		    }
		        .Chain();
                await chain();
		}

		protected async Task ExecuteCacheAdditionRules(CacheKey cacheKey,
			HttpRequestMessage request,
			HttpResponseMessage response)
		{
			var chain = new[]
				{
					AddCaching(cacheKey, request, response), // general adding caching
				}
				.Chain();
		    await chain();
		}

        protected virtual async Task CheckExpiryAsync(HttpRequestMessage request)
        {
            // not interested if not GET
            if(request.Method!=HttpMethod.Get)
                return;

            var cacheExpiry = CacheRefreshPolicyProvider(request, _configuration);
            if(cacheExpiry == TimeSpan.MaxValue)
                return; // infinity

            var cacheKey = GenerateCacheKey(request);
            TimedEntityTagHeaderValue value = await _entityTagStore.GetValueAsync(cacheKey);
            if(value == null)
                return;

            if (value.LastModified.Add(cacheExpiry) < DateTimeOffset.Now)
               await _entityTagStore.TryRemoveAsync(cacheKey);

        }

	    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			EnsureRulesSetup();

	        try
	        {
                // do the expiry
                await CheckExpiryAsync(request);

                HttpResponseMessage response = null;

	            foreach (var func in RequestInterceptionRules.Values)
	            {
	                response = await func(request);
                    if(response != null)
                        break;
	            }

	            if (response == null)
	            {
	                var resp = await base.SendAsync(request, cancellationToken);
	                return GetCachingContinuation(request)(resp);
	            }
	            else
	            {
                    return response;	                
	            }
	        }
	        catch (CacheCowServerException cacheCowServerException)
	        {               
                Trace.TraceWarning("Exception in CacheCow: " + cacheCowServerException.ToString());
	            throw;
	        }
           


			
		}

		/// <summary>
		/// This is a scenario where we have a POST to a resource
		/// and it needs to invalidate the cache to that resource
		/// and all its linked URLs
		/// 
		/// For example:
		/// POST /api/cars => invalidate /api/cars
		/// also it might invalidate /api/cars/fastest in which case
		/// /api/cars/fastest must be one of the linked URLs
		/// </summary>
		/// <param name="cacheKey">cacheKey</param>
		/// <param name="request">request</param>
		/// <param name="response">response</param>
		/// <returns>returns the function to execute</returns>
		internal Func<Task> PostInvalidationRule(
			CacheKey cacheKey,
			HttpRequestMessage request,
			HttpResponseMessage response)
		{
			return async () =>
			{
				if (request.Method != HttpMethod.Post)
					return;

				// if location header is set (for newly created resource), invalidate cache for it
				// this normally should not be necessary as the item is new and should not be in the cache
				// but releasing a non-existent item from cache should not have a big overhead
				if (response.Headers.Location != null)
				{
                    await InvalidateResourceAsync(new HttpRequestMessage(HttpMethod.Get, response.Headers.Location));
				}

			};
		}

		/// <summary>
		/// Adds caching for GET and PUT if 
		/// cache control provided is not null
		/// With PUT, since cache has been alreay invalidated,
		/// we provide the new ETag (old one has been cleared in invalidation phase)
		/// </summary>
		/// <param name="cacheKey"></param>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <param name="varyHeaders"></param>
		/// <returns></returns>
		internal Func<Task> AddCaching(
			CacheKey cacheKey,
			HttpRequestMessage request,
			HttpResponseMessage response)
		{
			return
				async () =>
				{

					var cacheControlHeaderValue = CacheControlHeaderProvider(request, _configuration);
					if (cacheControlHeaderValue == null)
						return;

					TimedEntityTagHeaderValue eTagValue;

					string uri = UriTrimmer(request.RequestUri);

					// in case of GET and no ETag
					// in case of PUT, we should return the new ETag of the resource
					// NOTE: No need to check if it is in the cache. If it were, it would not get
					// here
					if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Put)
					{
						// create new ETag only if it does not already exist
					    eTagValue = await _entityTagStore.GetValueAsync(cacheKey);
						if (eTagValue == null)
						{
							eTagValue = new TimedEntityTagHeaderValue(ETagValueGenerator(uri, request.Headers));
							await _entityTagStore.AddOrUpdateAsync(cacheKey, eTagValue);
						}

						// set ETag
						response.Headers.ETag = eTagValue.ToEntityTagHeaderValue();

						// set last-modified
						if (AddLastModifiedHeader && response.Content != null && !response.Content.Headers.Any(x => x.Key.Equals(HttpHeaderNames.LastModified,
							StringComparison.CurrentCultureIgnoreCase)))
						{
							response.Content.Headers.Add(HttpHeaderNames.LastModified, eTagValue.LastModified.ToString("r"));
						}

						// set Vary
						if (AddVaryHeader && _varyByHeaders != null && _varyByHeaders.Length > 0)
						{
							response.Headers.Add(HttpHeaderNames.Vary, _varyByHeaders);
						}

                        // harmonise Pragma header with cachecontrol header
                        if (cacheControlHeaderValue.NoStore)
                        {
                            response.Headers.TryAddWithoutValidation(HttpHeaderNames.Pragma, "no-cache");
                            if (response.Content != null)
                                response.Content.Headers.Expires = DateTimeOffset.Now.Subtract(TimeSpan.FromSeconds(1));
                        }
                        else
                        {
                            if (response.Headers.Contains(HttpHeaderNames.Pragma))
                                response.Headers.Remove(HttpHeaderNames.Pragma);                            
                        }

						response.Headers.TryAddWithoutValidation(HttpHeaderNames.CacheControl, cacheControlHeaderValue.ToString());
					}
				};
		}

		/// <summary>
		/// This invalidates the resource based on routePattern
		/// for methods POST, PUT and DELETE.
		/// It also removes for all linked URLs
		/// </summary>
		/// <param name="cacheKey"></param>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		internal Func<Task> InvalidateCache(
			CacheKey cacheKey,
			HttpRequestMessage request,
			HttpResponseMessage response)
		{
			return
				async () =>
				{
					if (!request.Method.Method.IsIn("PUT", "DELETE", "POST", "PATCH"))
						return;

                    // remove resource
                    await this.InvalidateResourceAsync(request);
                    
				};

		}

	    internal Func<HttpResponseMessage, HttpResponseMessage> GetCachingContinuation(HttpRequestMessage request)
		{
			return response =>
			{
				if (!response.IsSuccessStatusCode) // only if successful carry on processing
					return response;

			    try
			    {
                    var cacheKey = GenerateCacheKey(request);
                    ExecuteCacheInvalidationRules(cacheKey, request, response);
                    ExecuteCacheAdditionRules(cacheKey, request, response);
                    return response;
			    }
			    catch (Exception ex)
			    {
                   Trace.TraceWarning("Exception in CacheCow: " + ex.ToString());
                    return response;
			    }

			   
			};
		}

		private void EnsureRulesSetup()
		{
			if (RequestInterceptionRules == null)
			{
				lock (_padLock)
				{
					if (RequestInterceptionRules == null) // double if to prevent race condition
					{
						BuildRules();
					}
				}
			}
		}


		protected virtual void BuildRules()
		{
			RequestInterceptionRules = new Dictionary<string, Func<HttpRequestMessage, Task<HttpResponseMessage>>>();
			RequestInterceptionRules.Add("GetIfMatchNoneMatch", GetIfMatchNoneMatch());
			RequestInterceptionRules.Add("GetIfModifiedUnmodifiedSince", GetIfModifiedUnmodifiedSince());
			RequestInterceptionRules.Add("PutIfMatch", PutIfMatch());
			RequestInterceptionRules.Add("PutIfUnmodifiedSince", PutIfUnmodifiedSince());

		}

		internal Func<HttpRequestMessage, Task<HttpResponseMessage>> GetIfMatchNoneMatch()
		{
			return async (request) =>
			{
			    try
			    {
                    if (request.Method != HttpMethod.Get)
                        return null;

                    ICollection<EntityTagHeaderValue> noneMatchTags = request.Headers.IfNoneMatch;
                    ICollection<EntityTagHeaderValue> matchTags = request.Headers.IfMatch;

                    if (matchTags.Count == 0 && noneMatchTags.Count == 0)
                        return null; // no etag

                    if (matchTags.Count > 0 && noneMatchTags.Count > 0) // both if-match and if-none-match exist
                        return request.CreateResponse(HttpStatusCode.BadRequest);

                    var isNoneMatch = noneMatchTags.Count > 0;
                    var etags = isNoneMatch ? noneMatchTags : matchTags;

                    var entityTagKey = GenerateCacheKey(request);
                    // compare the Etag with the one in the cache
                    // do conditional get.
                    TimedEntityTagHeaderValue actualEtag = await _entityTagStore.GetValueAsync(entityTagKey);

                    bool matchFound = false;
                    if (actualEtag != null)
                    {
                        if (etags.Any(etag => etag.Tag == actualEtag.Tag))
                        {
                            matchFound = true;
                        }
                    }

			        return matchFound ^ isNoneMatch ? null : new NotModifiedResponse(request, null,
                        actualEtag.ToEntityTagHeaderValue());

			    }
			    catch (Exception e)
                {
                    throw new CacheCowServerException("Error in GetIfMatchNoneMatch", e);
			    }
			};

		}

		internal Func<HttpRequestMessage, Task<HttpResponseMessage>> GetIfModifiedUnmodifiedSince()
		{
            return async (request) =>
			{

			    try
			    {
                    if (request.Method != HttpMethod.Get)
                        return null;

                    DateTimeOffset? ifModifiedSince = request.Headers.IfModifiedSince;
                    DateTimeOffset? ifUnmodifiedSince = request.Headers.IfUnmodifiedSince;

                    if (ifModifiedSince == null && ifUnmodifiedSince == null)
                        return null; // no etag

                    if (ifModifiedSince != null && ifUnmodifiedSince != null) // both exist
                        return request.CreateResponse(HttpStatusCode.BadRequest);
                    bool ifModified = (ifUnmodifiedSince == null);
                    DateTimeOffset modifiedInQuestion = ifModified ? ifModifiedSince.Value : ifUnmodifiedSince.Value;


                    var entityTagKey = GenerateCacheKey(request);

                    TimedEntityTagHeaderValue actualEtag = await _entityTagStore.GetValueAsync(entityTagKey);

                    bool isModified = true;
                    if (actualEtag != null)
                    {
                        isModified = actualEtag.LastModified > modifiedInQuestion;
                    }

			        return isModified ^ ifModified
                            ? new NotModifiedResponse(request, null, actualEtag.ToEntityTagHeaderValue())
                            : null;

			    }
			    catch (Exception e)
			    {
                    throw new CacheCowServerException("Error in GetIfModifiedUnmodifiedSince", e);
			    }

			};
		}

		internal Func<HttpRequestMessage, Task<HttpResponseMessage>> PutIfUnmodifiedSince()
		{
			return async (request) =>
			{

			    try
			    {
                    if (request.Method != HttpMethod.Put)
                        return null;

                    DateTimeOffset? ifUnmodifiedSince = request.Headers.IfUnmodifiedSince;
                    if (ifUnmodifiedSince == null)
                        return null;

                    DateTimeOffset modifiedInQuestion = ifUnmodifiedSince.Value;

                    var entityTagKey = GenerateCacheKey(request);
                    TimedEntityTagHeaderValue actualEtag = null;

                    bool isModified = true;
                    if ((actualEtag = await _entityTagStore.GetValueAsync(entityTagKey)) != null)
                    {
                        isModified = actualEtag.LastModified > modifiedInQuestion;
                    }

                    return isModified ? request.CreateResponse(HttpStatusCode.PreconditionFailed) : null;
			    }
			    catch (Exception e)
			    {
                    throw new CacheCowServerException("Error in PutIfUnmodifiedSince", e);
			    }
			};
		}
		internal Func<HttpRequestMessage, Task<HttpResponseMessage>> PutIfMatch()
		{
			return async (request) =>
			{
			    try
			    {
                    if (request.Method != HttpMethod.Put)
                        return null;

                    ICollection<EntityTagHeaderValue> matchTags = request.Headers.IfMatch;
                    if (matchTags == null || matchTags.Count == 0)
                        return null;

                    var entityTagKey = GenerateCacheKey(request);
                    TimedEntityTagHeaderValue actualEtag = null;

                    bool matchFound = false;
                    if ((actualEtag = await _entityTagStore.GetValueAsync(entityTagKey)) != null)
                    {
                        if (matchTags.Any(etag => etag.Tag == actualEtag.Tag))
                        {
                            matchFound = true;
                        }
                    }

                    return matchFound ? null
                        : request.CreateResponse(HttpStatusCode.PreconditionFailed);
			    }
			    catch (Exception e)
			    {
                    throw new CacheCowServerException("Error in PutIfMatch", e);
                }
				

			};
		}

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(disposing)
                _entityTagStore.Dispose();
        }

	}
}
