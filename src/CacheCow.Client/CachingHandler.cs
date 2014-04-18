using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CacheCow.Client.Headers;
using CacheCow.Client.Internal;
using CacheCow.Common;
using CacheCow.Common.Helpers;

namespace CacheCow.Client
{
	public class CachingHandler : DelegatingHandler
	{

		private readonly ICacheStore _cacheStore;
		private Func<HttpRequestMessage, bool> _ignoreRequestRules;
		

		// 13.4: A response received with a status code of 200, 203, 206, 300, 301 or 410 MAY be stored 
		// TODO: Implement caching statuses other than 2xx
		private static HttpStatusCode[] _cacheableStatuses = new HttpStatusCode[]
		    {
				HttpStatusCode.OK, HttpStatusCode.NonAuthoritativeInformation,
       			HttpStatusCode.PartialContent, HttpStatusCode.MultipleChoices,
				HttpStatusCode.MovedPermanently, HttpStatusCode.Gone
			};

		public CachingHandler():this(new InMemoryCacheStore())
		{
		}

		public CachingHandler(ICacheStore cacheStore)
		{
			_cacheStore = cacheStore;
			UseConditionalPut = true;
		    MustRevalidateByDefault = true;
			VaryHeaderStore = new InMemoryVaryHeaderStore();
			DefaultVaryHeaders = new string[]{"Accept"};
			ResponseValidator = (response) =>
			    {
					// 13.4
					//Unless specifically constrained by a cache-control (section 14.9) directive, a caching system MAY always store 
					// a successful response (see section 13.8) as a cache entry, MAY return it without validation if it 
					// is fresh, and MAY return it after successful validation. If there is neither a cache validator nor an 
					// explicit expiration time associated with a response, we do not expect it to be cached, but certain caches MAY violate this expectation 
					// (for example, when little or no network connectivity is available).

                    // 14.9.1
                    // If the no-cache directive does not specify a field-name, then a cache MUST NOT use the response to satisfy a subsequent request without 
                    // successful revalidation with the origin server. This allows an origin server to prevent caching 
                    // even by caches that have been configured to return stale responses to client requests.
                    //If the no-cache directive does specify one or more field-names, then a cache MAY use the response 
                    // to satisfy a subsequent request, subject to any other restrictions on caching. However, the specified 
                    // field-name(s) MUST NOT be sent in the response to a subsequent request without successful revalidation 
                    // with the origin server. This allows an origin server to prevent the re-use of certain header fields in a response, while still allowing caching of the rest of the response.
					if (!response.StatusCode.IsIn(_cacheableStatuses))
						return ResponseValidationResult.NotCacheable;

					if (!response.IsSuccessStatusCode || response.Headers.CacheControl == null ||
                        response.Headers.CacheControl.NoStore) //  || response.Headers.CacheControl.NoCache was removed. See issue
						return ResponseValidationResult.NotCacheable;

					response.Headers.Date = response.Headers.Date ?? DateTimeOffset.UtcNow; // this also helps in cache creation
			        var dateTimeOffset = response.Headers.Date;

					if(response.Content == null)
						return ResponseValidationResult.NotCacheable;

					if (response.Headers.CacheControl.MaxAge == null &&
						response.Headers.CacheControl.SharedMaxAge == null &&
						response.Content.Headers.Expires == null)
						return ResponseValidationResult.NotCacheable;

                    if(response.Headers.CacheControl.NoCache)
                        return ResponseValidationResult.MustRevalidate;

                    // here we use 
					if (response.Content.Headers.Expires != null &&
						response.Content.Headers.Expires < DateTimeOffset.UtcNow)
						return response.Headers.CacheControl.ShouldRevalidate(MustRevalidateByDefault) 
                            ? ResponseValidationResult.MustRevalidate : ResponseValidationResult.Stale;					

					if (response.Headers.CacheControl.MaxAge != null &&
						DateTimeOffset.UtcNow > response.Headers.Date.Value.Add(response.Headers.CacheControl.MaxAge.Value))
                        return response.Headers.CacheControl.ShouldRevalidate(MustRevalidateByDefault)
                            ? ResponseValidationResult.MustRevalidate : ResponseValidationResult.Stale;					

					if (response.Headers.CacheControl.SharedMaxAge != null &&
						DateTimeOffset.UtcNow > response.Headers.Date.Value.Add(response.Headers.CacheControl.SharedMaxAge.Value))
                        return response.Headers.CacheControl.ShouldRevalidate(MustRevalidateByDefault)
                            ? ResponseValidationResult.MustRevalidate : ResponseValidationResult.Stale;					

			        return ResponseValidationResult.OK;
			    };

			_ignoreRequestRules = (request) =>
			    {

					if (!request.Method.IsIn(HttpMethod.Get, HttpMethod.Put))
						return true;

					// client can tell CachingHandler not to do caching for a particular request
					if(request.Headers.CacheControl!=null)
					{
						if (request.Headers.CacheControl.NoStore)
							return true;
					}

			        return false;
			    };

			ResponseStoragePreparationRules = (response) =>
			    {
					// 14.9.3
					// If a response includes both an Expires header and a max-age directive, 
					// the max-age directive overrides the Expires header, even if the Expires header is more restrictive.
					if(response.Content.Headers.Expires!=null &&
						(response.Headers.CacheControl.MaxAge != null || response.Headers.CacheControl.SharedMaxAge!=null))
					{
						response.Content.Headers.Expires = null;
					}
			    };
		}

		public IVaryHeaderStore VaryHeaderStore { get; set; }

		public string[] DefaultVaryHeaders { get; set; }

		public string[] StarVaryHeaders { get; set; } // TODO: populate and use

		/// <summary>
		/// Whether to use cache's ETag or Last-Modified
		/// to make conditional PUT according to RFC2616 13.3
		/// If no cache available on the resource, no conditional is used
		/// </summary>
		public bool UseConditionalPut { get; set; }

        /// <summary>
        /// true by default;
        /// If true, then as soon as a resource is stale, GET calls will always be
        /// conditional GET regardless of presence of must-revalidate in the response.
        /// If false, conditional GET is called only if max-age defined by request or
        /// must-revalidate is defined in the response.
        /// </summary>
        public bool MustRevalidateByDefault { get; set; }

		/// <summary>
		/// Inspects the response and returns ResponseValidationResult
		/// based on the rules defined
		/// </summary>
		public Func<HttpResponseMessage, ResponseValidationResult> ResponseValidator { get; set; }

		/// <summary>
		/// Applies a few rules and prepares the response
		/// for storage in the CacheStore
		/// </summary>
		public Action<HttpResponseMessage> ResponseStoragePreparationRules { get; set; } 


        /// <summary>
        /// Returns whether resource is fresh or if stale, it is acceptable to be stale
        /// null --> dont know, cannot be determined
        /// true --> yes, is OK if stale
        /// false --> no, it is not OK to be stale 
        /// </summary>
        /// <param name="cachedResponse"></param>
        /// <param name="request"></param>
        /// <returns></returns>
		private bool? IsFreshOrStaleAcceptable(HttpResponseMessage cachedResponse, HttpRequestMessage request)
		{

			TimeSpan staleness = TimeSpan.Zero; // negative = fresh, positive = stale

			if(cachedResponse==null)
				throw new ArgumentNullException("cachedResponse");

			if(request==null)
				throw new ArgumentNullException("request");

			if (cachedResponse.Content == null)
				return null;		

			DateTimeOffset? responseDate = cachedResponse.Headers.Date ?? cachedResponse.Content.Headers.LastModified; // Date should have a value
			if (responseDate == null)
				return null;

			if (cachedResponse.Headers.CacheControl == null)
				return null;

			// calculating staleness
			// according to http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.9.3 max-age overrides expires header
			if (cachedResponse.Content.Headers.Expires != null)
			{
				staleness = DateTimeOffset.Now.Subtract(cachedResponse.Content.Headers.Expires.Value);
			}

			if (cachedResponse.Headers.CacheControl.MaxAge.HasValue) // Note: this is MaxAge for response
			{
				staleness = DateTimeOffset.Now.Subtract(responseDate.Value.Add(cachedResponse.Headers.CacheControl.MaxAge.Value));
			}

			if (request.Headers.CacheControl == null)
				return staleness < TimeSpan.Zero;

			if (request.Headers.CacheControl.MinFresh.HasValue)
				return -staleness > request.Headers.CacheControl.MinFresh.Value; // staleness is negative if still fresh

			if (request.Headers.CacheControl.MaxStale) // stale acceptable
				return true;

			if (request.Headers.CacheControl.MaxStaleLimit.HasValue)
				return staleness < request.Headers.CacheControl.MaxStaleLimit.Value;

			if (request.Headers.CacheControl.MaxAge.HasValue)
				return responseDate.Value.Add(request.Headers.CacheControl.MaxAge.Value) > DateTimeOffset.Now;

			return false;
		}

        // TODO: this method is terribly long. Shorten
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var cacheCowHeader = new CacheCowHeader();
			string uri = request.RequestUri.ToString();

			TraceWriter.WriteLine("{0} - Starting", TraceLevel.Verbose, request.RequestUri.ToString());


			// check if needs to be ignored
			if (_ignoreRequestRules(request))
				return base.SendAsync(request, cancellationToken); // EXIT !! _________________


			IEnumerable<string> varyHeaders;
			if(!VaryHeaderStore.TryGetValue(uri, out varyHeaders))
			{
				varyHeaders = DefaultVaryHeaders;
			}
			var cacheKey = new CacheKey(uri, 
				request.Headers.Where(x=> varyHeaders.Any(y=> y.Equals(x.Key, 
					StringComparison.CurrentCultureIgnoreCase)))
					.SelectMany(z=>z.Value)
				);

			// get from cache and verify response
			HttpResponseMessage cachedResponse;
			ResponseValidationResult validationResultForCachedResponse = ResponseValidationResult.NotExist;

			TraceWriter.WriteLine("{0} - Before TryGetValue", TraceLevel.Verbose, request.RequestUri.ToString());

			cacheCowHeader.DidNotExist = !_cacheStore.TryGetValue(cacheKey, out cachedResponse);
			TraceWriter.WriteLine("{0} - After TryGetValue", TraceLevel.Verbose, request.RequestUri.ToString());

			if (!cacheCowHeader.DidNotExist.Value) // so if it EXISTS in cache
			{
				cachedResponse.RequestMessage = request;
				validationResultForCachedResponse = ResponseValidator(cachedResponse);
			}

			TraceWriter.WriteLine("{0} - After ResponseValidator {1}",
				TraceLevel.Verbose, request.RequestUri, validationResultForCachedResponse);


			// PUT validation
			if (request.Method == HttpMethod.Put && validationResultForCachedResponse.IsIn(
				 ResponseValidationResult.OK, ResponseValidationResult.MustRevalidate))
			{
				// add headers for a cache validation. First check ETag since is better 
				if (UseConditionalPut)
				{
					cacheCowHeader.CacheValidationApplied = true;
					if (cachedResponse.Headers.ETag != null)
					{
						request.Headers.Add(HttpHeaderNames.IfMatch,
							cachedResponse.Headers.ETag.ToString());
					}
					else if (cachedResponse.Content.Headers.LastModified != null)
					{
						request.Headers.Add(HttpHeaderNames.IfUnmodifiedSince,
							cachedResponse.Content.Headers.LastModified.Value.ToString("r"));
					}
				}
				return base.SendAsync(request, cancellationToken); // EXIT !! _____________________________
			}

			// here onward is only GET only. See if cache OK and if it is then return
			if (validationResultForCachedResponse == ResponseValidationResult.OK)
			{
				cacheCowHeader.RetrievedFromCache = true;
				return TaskHelpers.FromResult(cachedResponse.AddCacheCowHeader(cacheCowHeader)); // EXIT !! ____________________________				
			} 
			
			// if stale
			else if(validationResultForCachedResponse == ResponseValidationResult.Stale)
			{
				cacheCowHeader.WasStale = true;
				var isFreshOrStaleAcceptable = IsFreshOrStaleAcceptable(cachedResponse, request);
			    if (isFreshOrStaleAcceptable.HasValue && isFreshOrStaleAcceptable.Value) // similar to OK
			    {
                    // TODO: CONSUME AND RELEASE Response !!!
                    return TaskHelpers.FromResult(cachedResponse.AddCacheCowHeader(cacheCowHeader));
			            // EXIT !! ____________________________				
			    }
			        
			    else
			        validationResultForCachedResponse = ResponseValidationResult.MustRevalidate; // revalidate

			}

			// cache validation for GET
			else if(validationResultForCachedResponse == ResponseValidationResult.MustRevalidate)
			{
				cacheCowHeader.CacheValidationApplied = true;
                cacheCowHeader.WasStale = true;

				// add headers for a cache validation. First check ETag since is better 
				if(cachedResponse.Headers.ETag!=null)
				{
					request.Headers.Add(HttpHeaderNames.IfNoneMatch,
						cachedResponse.Headers.ETag.ToString());					
				}
				else if(cachedResponse.Content.Headers.LastModified!=null)
				{
					request.Headers.Add(HttpHeaderNames.IfModifiedSince,
						cachedResponse.Content.Headers.LastModified.Value.ToString("r"));
				}

			}

			// _______________________________ RESPONSE only GET  ___________________________________________

			return base.SendAsync(request, cancellationToken)
				.ContinueWith(
				tt =>
					{
						var serverResponse = tt.Result;
						TraceWriter.WriteLine("{0} - After getting response", 
							TraceLevel.Verbose, request.RequestUri.ToString());

						
						if (request.Method != HttpMethod.Get) // only interested here if it is a GET - this line really never called - only GET gets here
							return serverResponse;

						// in case of MustRevalidate with result 304
						if(validationResultForCachedResponse == ResponseValidationResult.MustRevalidate && 
							serverResponse.StatusCode == HttpStatusCode.NotModified)
						{
							cachedResponse.RequestMessage = request;
							cacheCowHeader.RetrievedFromCache = true;
							TraceWriter.WriteLine("{0} - NotModified",
								TraceLevel.Verbose, request.RequestUri.ToString());

                            ConsumeAndDisposeResponse(serverResponse);
							return cachedResponse.AddCacheCowHeader(cacheCowHeader); // EXIT !! _______________
						}

						var validationResult = ResponseValidator(serverResponse);
						switch (validationResult)
						{
							case ResponseValidationResult.MustRevalidate:
							case ResponseValidationResult.OK:

								TraceWriter.WriteLine("{0} - ResponseValidationResult.OK or MustRevalidate",
									TraceLevel.Verbose, request.RequestUri.ToString());


								// prepare
								ResponseStoragePreparationRules(serverResponse);

								TraceWriter.WriteLine("{0} - Before AddOrUpdate", TraceLevel.Verbose, request.RequestUri.ToString());

								// store the cache
								_cacheStore.AddOrUpdate(cacheKey, serverResponse);

								TraceWriter.WriteLine("{0} - Before AddOrUpdate", TraceLevel.Verbose, request.RequestUri.ToString());

								// if there is a vary header, store it
								if(serverResponse.Headers.Vary!=null)
									VaryHeaderStore.AddOrUpdate(uri, serverResponse.Headers.Vary.Select(x=>x).ToArray());
								break;
							default:
								TraceWriter.WriteLine("{0} - ResponseValidationResult. Other",
									TraceLevel.Verbose, request.RequestUri.ToString());

									TraceWriter.WriteLine("{0} - Before TryRemove", TraceLevel.Verbose, request.RequestUri.ToString());
								_cacheStore.TryRemove(cacheKey);
								TraceWriter.WriteLine("{0} - After AddOrUpdate", TraceLevel.Verbose, request.RequestUri.ToString());

								cacheCowHeader.NotCacheable = true;


								break;
						}
						TraceWriter.WriteLine("{0} - Before returning response",
							TraceLevel.Verbose, request.RequestUri.ToString());

						return serverResponse.AddCacheCowHeader(cacheCowHeader);
					}
				);
		}

        private void ConsumeAndDisposeResponse(HttpResponseMessage response)
        {
            response.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                VaryHeaderStore.Dispose();
                var disposable = _cacheStore as IDisposable;
                if(disposable!=null)
                    disposable.Dispose();
            }
        }

	}
}
