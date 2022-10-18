using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private bool _disposeCacheStore = false;
        private bool _disposeVaryStore = false;


        // 13.4: A response received with a status code of 200, 203, 206, 300, 301 or 410 MAY be stored
        // TODO: Implement caching statuses other than 2xx
        private static HttpStatusCode[] _cacheableStatuses = new HttpStatusCode[]
        {
        HttpStatusCode.OK, HttpStatusCode.NonAuthoritativeInformation,
             HttpStatusCode.PartialContent, HttpStatusCode.MultipleChoices,
        HttpStatusCode.MovedPermanently, HttpStatusCode.Gone
      };

        public CachingHandler()
            : this(new InMemoryCacheStore())
        {
            _disposeCacheStore = true;
        }

        public CachingHandler(ICacheStore cacheStore)
            : this(cacheStore, new InMemoryVaryHeaderStore())
        {
            _disposeVaryStore = true;
        }

        public CachingHandler(ICacheStore cacheStore, IVaryHeaderStore varyHeaderStore)
        {
            _cacheStore = cacheStore;
            UseConditionalPutPatchDelete = true;
            MustRevalidateByDefault = true;
            VaryHeaderStore = varyHeaderStore;
            DefaultVaryHeaders = new string[] { HttpHeaderNames.Accept };
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

                // Technically any response is cacheable unless we are told so or some rules
                // but we DO NOT deem cacheable a response which does not bother to put CacheControl header
                if (!response.IsSuccessStatusCode || response.Headers.CacheControl == null ||
                    response.Headers.CacheControl.NoStore) //  || response.Headers.CacheControl.NoCache was removed. See issue
                    return ResponseValidationResult.NotCacheable;

                if (response.Headers.Date == null)
                    TraceWriter.WriteLine("Response date is NULL", TraceLevel.Warning);

                response.Headers.Date = response.Headers.Date ?? DateTimeOffset.UtcNow; // this also helps in cache creation
                var dateTimeOffset = response.Headers.Date;
                var age = TimeSpan.Zero;
                if (response.Headers.Age.HasValue)
                    age = response.Headers.Age.Value;

                TraceWriter.WriteLine(
                    String.Format("CachedResponse date was => {0} - compared to UTC.Now => {1}", dateTimeOffset, DateTimeOffset.UtcNow), TraceLevel.Verbose);

                if (response.Content == null)
                    return ResponseValidationResult.NotCacheable;

                if (response.Headers.CacheControl.MaxAge == null &&
                    response.Headers.CacheControl.SharedMaxAge == null &&
                    response.Content.Headers.Expires == null)
                    return ResponseValidationResult.NotCacheable;

                if (response.Headers.CacheControl.NoCache)
                    return ResponseValidationResult.MustRevalidate;

                if (response.RequestMessage?.Headers?.CacheControl != null &&
                    response.RequestMessage.Headers.CacheControl.NoCache)
                    return ResponseValidationResult.MustRevalidate;

                if (response.Headers.CacheControl.MaxAge != null &&
                    DateTimeOffset.UtcNow > response.Headers.Date.Value.Add(response.Headers.CacheControl.MaxAge.Value.Subtract(age)))
                    return response.Headers.CacheControl.ShouldRevalidate(MustRevalidateByDefault)
                        ? ResponseValidationResult.MustRevalidate : ResponseValidationResult.Stale;

                if (response.Headers.CacheControl.SharedMaxAge != null &&
                    DateTimeOffset.UtcNow > response.Headers.Date.Value.Add(response.Headers.CacheControl.SharedMaxAge.Value.Subtract(age)))
                    return response.Headers.CacheControl.ShouldRevalidate(MustRevalidateByDefault)
                        ? ResponseValidationResult.MustRevalidate : ResponseValidationResult.Stale;

                // moved this down since Expires is < MaxAge
                if (response.Content.Headers.Expires != null &&
                    response.Content.Headers.Expires < DateTimeOffset.UtcNow)
                    return response.Headers.CacheControl.ShouldRevalidate(MustRevalidateByDefault)
                        ? ResponseValidationResult.MustRevalidate : ResponseValidationResult.Stale;

                return ResponseValidationResult.OK;
            };

            _ignoreRequestRules = (request) =>
            {

                if (request.Method.IsCacheIgnorable())
                    return true;

                // client can tell CachingHandler not to do caching for a particular request
                if (request.Headers.CacheControl != null)
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
                if (response.Content.Headers.Expires != null &&
                    (response.Headers.CacheControl.MaxAge != null || response.Headers.CacheControl.SharedMaxAge != null))
                {
                    response.Content.Headers.Expires = null;
                }
            };

        }

        static CachingHandler()
        {
            IgnoreExceptionPolicy = (e) => { };
        }

        public IVaryHeaderStore VaryHeaderStore { get; set; }

        public string[] DefaultVaryHeaders { get; set; }

        public string[] StarVaryHeaders { get; set; } // TODO: populate and use

        /// <summary>
        /// Whether to use cache's ETag or Last-Modified
        /// to make conditional PUT/PATCH/DELETE according to RFC2616 13.3
        /// If no cache available on the resource, no conditional is used
        /// </summary>
        public bool UseConditionalPutPatchDelete { get; set; }

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
        /// If set to true, it does not emit CacheCowHeader
        /// </summary>
        public bool DoNotEmitCacheCowHeader { get; set; } = false;

        /// <summary>
        /// Ignores all exceptions. Set it to ExceptionHandler
        /// </summary>
        public static Action<Exception> IgnoreExceptionPolicy { get; private set; }

        /// <summary>
        /// Returns whether resource is fresh or if stale, it is acceptable to be stale
        /// null --> dont know, cannot be determined
        /// true --> yes, is OK if stale
        /// false --> no, it is not OK to be stale
        /// </summary>
        /// <param name="cachedResponse"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        internal static bool? IsFreshOrStaleAcceptable(HttpResponseMessage cachedResponse, HttpRequestMessage request)
        {

            TimeSpan staleness = TimeSpan.Zero; // negative = fresh, positive = stale
            TimeSpan age = TimeSpan.Zero;

            if (cachedResponse == null)
                throw new ArgumentNullException("cachedResponse");

            if (request == null)
                throw new ArgumentNullException("request");

            if (cachedResponse.Content == null)
                return null;

            if (cachedResponse.Headers.Age.HasValue)
                age = cachedResponse.Headers.Age.Value;

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
                staleness = DateTimeOffset.Now.Subtract(responseDate.Value.Subtract(age).Add(cachedResponse.Headers.CacheControl.MaxAge.Value));
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
                return responseDate.Value.Subtract(age).Add(request.Headers.CacheControl.MaxAge.Value) > DateTimeOffset.Now;

            return false;
        }

        // TODO: this method is terribly long. Shorten
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cacheCowHeader = new CacheCowHeader();
            string uri = request.RequestUri.ToString();
            var originalHeaders = request.Headers.ToList();

            TraceWriter.WriteLine("{0} - Starting SendAsync", TraceLevel.Verbose, uri);

            // check if needs to be ignored
            if (_ignoreRequestRules(request))
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false); // EXIT !! _________________

            IEnumerable<string> varyHeaders;
            if (!VaryHeaderStore.TryGetValue(uri, out varyHeaders))
            {
                varyHeaders = DefaultVaryHeaders;
            }

            var cacheKey = new CacheKey(uri,
                originalHeaders.Where(x => varyHeaders.Any(y => y.Equals(x.Key,
                    StringComparison.CurrentCultureIgnoreCase)))
                    .SelectMany(z => z.Value)
                );

            // get from cache and verify response
            HttpResponseMessage cachedResponse;
            var validationResultForCachedResponse = ResponseValidationResult.NotExist; // default

            TraceWriter.WriteLine("{0} - Before TryGetValue", TraceLevel.Verbose, uri);

            cachedResponse = await _cacheStore.GetValueAsync(cacheKey).ConfigureAwait(false);
            cacheCowHeader.DidNotExist = cachedResponse == null;
            TraceWriter.WriteLine("{0} - After TryGetValue: DidNotExist => {1}", TraceLevel.Verbose,
                uri, cacheCowHeader.DidNotExist);

            if (!cacheCowHeader.DidNotExist.Value) // so if it EXISTS in cache
            {
                TraceWriter.WriteLine("{0} - Existed in the cache. CacheControl Headers => {1}", TraceLevel.Verbose, uri,
                    cachedResponse.Headers.CacheControl.ToString());
                cachedResponse.RequestMessage = request;
                validationResultForCachedResponse = ResponseValidator(cachedResponse);
            }

            TraceWriter.WriteLine("{0} - After ResponseValidator => {1}",
                TraceLevel.Verbose, request.RequestUri, validationResultForCachedResponse);


            // PUT/PATCH/DELETE validation
            if (request.Method.IsPutPatchOrDelete() && validationResultForCachedResponse.IsIn(
                 ResponseValidationResult.OK, ResponseValidationResult.MustRevalidate))
            {
                ApplyPutPatchDeleteValidationHeaders(request, cacheCowHeader, cachedResponse);
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false); // EXIT !! _____________________________
            }

            // here onward is only GET only. See if cache OK and if it is then return
            if (validationResultForCachedResponse == ResponseValidationResult.OK)
            {
                cacheCowHeader.RetrievedFromCache = true;
                if (!DoNotEmitCacheCowHeader)
                    cachedResponse.AddCacheCowHeader(cacheCowHeader);
                return cachedResponse; // EXIT !! ____________________________
            }

            // if stale
            else if (validationResultForCachedResponse == ResponseValidationResult.Stale)
            {
                cacheCowHeader.WasStale = true;
                var isFreshOrStaleAcceptable = IsFreshOrStaleAcceptable(cachedResponse, request);
                if (isFreshOrStaleAcceptable.HasValue && isFreshOrStaleAcceptable.Value) // similar to OK
                {
                    // TODO: CONSUME AND RELEASE Response !!!
                    if (!DoNotEmitCacheCowHeader)
                        cachedResponse.AddCacheCowHeader(cacheCowHeader);
                    return cachedResponse;
                    // EXIT !! ____________________________
                }
                else
                    validationResultForCachedResponse = ResponseValidationResult.MustRevalidate; // revalidate
            }

            // cache validation for GET
            else if (validationResultForCachedResponse == ResponseValidationResult.MustRevalidate)
            {
                ApplyGetCacheValidationHeaders(request, cacheCowHeader, cachedResponse);
            }


            // _______________________________ RESPONSE only GET  ___________________________________________

            var serverResponse = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (serverResponse.Content != null)
            {
                // these two prevent serialisation without ContentLength which barfs for chunked encoding - issue #267
                TraceWriter.WriteLine($"Content Size: {serverResponse.Content.Headers.ContentLength}", TraceLevel.Verbose);
                if (serverResponse.Content.Headers.ContentType == null)
                {
                    serverResponse.Content.Headers.Add("Content-Type", "application/octet-stream");
                }
            }

            // HERE IS LATE FOR APPLYING EXCEPTION POLICY !!!

            TraceWriter.WriteLine("{0} - After getting response",
                TraceLevel.Verbose, uri);

            if (request.Method != HttpMethod.Get) // only interested here if it is a GET - this line really never called - only GET gets here
                return serverResponse;

            // in case of MustRevalidate with result 304
            if (validationResultForCachedResponse == ResponseValidationResult.MustRevalidate &&
                serverResponse.StatusCode == HttpStatusCode.NotModified)
            {
                TraceWriter.WriteLine("{0} - Got 304 from the server and ResponseValidationResult.MustRevalidate",
                    TraceLevel.Verbose, uri);

                cachedResponse.RequestMessage = request;
                cacheCowHeader.RetrievedFromCache = true;
                TraceWriter.WriteLine("{0} - NotModified",
                    TraceLevel.Verbose, uri);

                await UpdateCachedResponseAsync(cacheKey, cachedResponse, serverResponse, _cacheStore).ConfigureAwait(false);
                ConsumeAndDisposeResponse(serverResponse);
                if (!DoNotEmitCacheCowHeader)
                    cachedResponse.AddCacheCowHeader(cacheCowHeader).CopyOtherCacheCowHeaders(serverResponse);
                return cachedResponse;
                // EXIT !! _______________
            }

            var validationResult = ResponseValidator(serverResponse);
            switch (validationResult)
            {
                case ResponseValidationResult.MustRevalidate:
                case ResponseValidationResult.OK:

                    TraceWriter.WriteLine("{0} - ResponseValidationResult.OK or MustRevalidate",
                        TraceLevel.Verbose, uri);


                    // prepare
                    ResponseStoragePreparationRules(serverResponse);

                    // re-create cacheKey with real server accept

                    // if there is a vary header, store it
                    if (serverResponse.Headers.Vary?.Any() ?? false)
                    {
                        varyHeaders = serverResponse.Headers.Vary.Select(x => x).ToArray();
                        IEnumerable<string> temp;
                        if (!VaryHeaderStore.TryGetValue(uri, out temp))
                        {
                            VaryHeaderStore.AddOrUpdate(uri, varyHeaders);
                        }
                    }

                    // create real cacheKey with correct Vary headers
                    cacheKey = new CacheKey(uri,
                        originalHeaders.Where(x => varyHeaders.Any(y => y.Equals(x.Key,
                            StringComparison.CurrentCultureIgnoreCase)))
                            .SelectMany(z => z.Value)
                        );

                    // store the cache
                    CheckForCacheCowHeader(serverResponse);
                    await _cacheStore.AddOrUpdateAsync(cacheKey, serverResponse).ConfigureAwait(false);

                    TraceWriter.WriteLine("{0} - After AddOrUpdate", TraceLevel.Verbose, uri);


                    break;
                default:
                    TraceWriter.WriteLine("{0} - ResponseValidationResult. Other",
                        TraceLevel.Verbose, uri);

                    TraceWriter.WriteLine("{0} - Before TryRemove", TraceLevel.Verbose, uri);
                    await _cacheStore.TryRemoveAsync(cacheKey);
                    TraceWriter.WriteLine("{0} - After TryRemoveAsync", TraceLevel.Verbose, uri);

                    cacheCowHeader.NotCacheable = true;

                    break;
            }
            TraceWriter.WriteLine("{0} - Before returning response",
                TraceLevel.Verbose, request.RequestUri.ToString());

            if (!DoNotEmitCacheCowHeader)
                serverResponse.AddCacheCowHeader(cacheCowHeader);

            return serverResponse;
        }

        private void ApplyPutPatchDeleteValidationHeaders(HttpRequestMessage request, CacheCowHeader cacheCowHeader,
            HttpResponseMessage cachedResponse)
        {
            // add headers for a cache validation. First check ETag since is better
            if (UseConditionalPutPatchDelete)
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
        }

        internal async static Task UpdateCachedResponseAsync(CacheKey cacheKey,
            HttpResponseMessage cachedResponse,
            HttpResponseMessage serverResponse,
            ICacheStore store)
        {
            TraceWriter.WriteLine("CachingHandler.UpdateCachedResponseAsync - response: " + serverResponse.Headers.ToString(), TraceLevel.Verbose);

            // update only if server had a cachecontrol.
            // TODO: merge CacheControl headers instead of replace
            if (serverResponse.Headers.CacheControl != null && (!serverResponse.Headers.CacheControl.NoCache)) // added to cover issue #139
            {
                TraceWriter.WriteLine("CachingHandler.UpdateCachedResponseAsync - CacheControl: " + serverResponse.Headers.CacheControl.ToString(), TraceLevel.Verbose);
                cachedResponse.Headers.CacheControl = serverResponse.Headers.CacheControl;
            }
            else
            {
                TraceWriter.WriteLine("CachingHandler.UpdateCachedResponseAsync - CacheControl missing from server. Applying sliding expiration. Date => " + DateTimeOffset.UtcNow, TraceLevel.Verbose);
            }

            cachedResponse.Headers.Date = DateTimeOffset.UtcNow; // very important
            CheckForCacheCowHeader(cachedResponse);
            await store.AddOrUpdateAsync(cacheKey, cachedResponse).ConfigureAwait(false);
        }

        private static void CheckForCacheCowHeader(HttpResponseMessage responseMessage)
        {
            var header = responseMessage.Headers.GetCacheCowHeader();
            if (header != null)
            {
                TraceWriter.WriteLine("!!WARNING!! response stored with CacheCowHeader!!", TraceLevel.Warning);
            }
        }

        private static void ApplyGetCacheValidationHeaders(HttpRequestMessage request, CacheCowHeader cacheCowHeader,
            HttpResponseMessage cachedResponse)
        {
            cacheCowHeader.CacheValidationApplied = true;
            cacheCowHeader.WasStale = true;

            // add headers for a cache validation. First check ETag since is better
            if (cachedResponse.Headers.ETag != null)
            {
                request.Headers.Add(HttpHeaderNames.IfNoneMatch,
                    cachedResponse.Headers.ETag.ToString());
            }
            else if (cachedResponse.Content.Headers.LastModified != null)
            {
                request.Headers.Add(HttpHeaderNames.IfModifiedSince,
                    cachedResponse.Content.Headers.LastModified.Value.ToString("r"));
            }
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
                if (VaryHeaderStore != null && _disposeVaryStore)
                    VaryHeaderStore.Dispose();

                if (_cacheStore != null && _disposeCacheStore)
                    _cacheStore.Dispose();
            }
        }

    }
}
