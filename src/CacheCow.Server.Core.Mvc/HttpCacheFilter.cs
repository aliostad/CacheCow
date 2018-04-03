using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheCow.Common;
using CacheCow.Common.Helpers;
using CacheCow.Server.Core;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace CacheCow.Server.Core.Mvc
{
    /// <summary>
    /// A resource filter responsibility for implementing HTTP Caching
    /// Use it with HttpCacheFactoryAttribute
    /// </summary>
    public class HttpCacheFilter : IAsyncResourceFilter
    {
        private ICacheabilityValidator _validator;
        private readonly ISerialiser _serialiser;
        private readonly ITimedETagExtractor _timedETagExtractor;
        private readonly ITimedETagQueryProvider _timedETagQueryProvider;

        public HttpCacheFilter(ICacheabilityValidator validator,
            ISerialiser serialiser,
            ICacheDirectiveProvider cacheDirectiveProvider,
            ITimedETagExtractor timedETagExtractor,
            ITimedETagQueryProvider timedETagQueryProvider)
        {
            _validator = validator;
            _serialiser = serialiser;
            CacheDirectiveProvider = cacheDirectiveProvider;
            _timedETagExtractor = timedETagExtractor;
            _timedETagQueryProvider = timedETagQueryProvider;
            ApplyNoCacheNoStoreForNonCacheableResponse = true;
        }

        /// <summary>
        /// Happens at the incoming (executING)
        /// </summary>
        /// <param name="timedEtag"></param>
        /// <param name="cacheValidationStatus"></param>
        /// <param name="context">
        /// </param>
        /// <returns>
        /// True: applied and the call can exit 
        /// False: tried to apply but did not match hence the call should continue
        /// null: could not apply (timedEtag was null)
        /// </returns>
        protected bool? ApplyCacheValidation(TimedEntityTagHeaderValue timedEtag,
            CacheValidationStatus cacheValidationStatus,
            ResourceExecutingContext context)
        {
            if (timedEtag == null)
                return null;

            var headers = context.HttpContext.Request.GetTypedHeadersWithCaching();
            switch (cacheValidationStatus)
            {
                case CacheValidationStatus.GetIfModifiedSince:
                    if (timedEtag.LastModified == null)
                        return false;
                    else
                    {
                        if (timedEtag.LastModified > headers.IfModifiedSince.Value)
                            return false;
                        else
                        {
                            context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
                            return true;
                        }
                    }

                case CacheValidationStatus.GetIfNoneMatch:
                    if (timedEtag.ETag == null)
                        return false;
                    else
                    {
                        if (headers.IfNoneMatch.Any(x => x.Tag == timedEtag.ETag.Tag))
                        {
                            context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
                            return true;
                        }
                        else
                            return false;
                    }
                case CacheValidationStatus.PutIfMatch:
                    if (timedEtag.ETag == null)
                        return false;
                    else
                    {
                        if (headers.IfMatch.Any(x => x.Tag == timedEtag.ETag.Tag))
                            return false;
                        else
                        {
                            context.Result = new StatusCodeResult(StatusCodes.Status409Conflict);
                            return true;
                        }
                    }
                case CacheValidationStatus.PutIfUnModifiedSince:
                    if (timedEtag.LastModified == null)
                        return false;
                    else
                    {
                        if (timedEtag.LastModified > headers.IfUnmodifiedSince.Value)
                        {
                            context.Result = new StatusCodeResult(StatusCodes.Status409Conflict);
                            return true;
                        }
                        else
                            return false;
                    }

                default:
                    return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            bool? cacheValidated = null;
            bool isRequestCacheable = _validator.IsCacheable(context.HttpContext.Request);
            var cacheValidationStatus = context.HttpContext.Request.GetCacheValidationStatus();
            if (cacheValidationStatus != CacheValidationStatus.None)
            {
                var timedETag = await _timedETagQueryProvider.QueryAsync(context);
                cacheValidated = ApplyCacheValidation(timedETag, cacheValidationStatus, context);
                if (cacheValidated ?? false)
                {
                    // the response would have been set and no need to run the rest of the pipeline
                    return;
                }
            }

            var execCtx = await next(); // _______________________________________________________________________________

            if (execCtx.Canceled)
                return;

            var or = execCtx.Result as ObjectResult;
            TimedEntityTagHeaderValue tet = null;
            if (or != null && or.Value != null)
            {
                tet = GetTimedETagFromResult(execCtx, or.Value);
            }

            if (cacheValidated == null  // could not validate
                && tet != null
                && (cacheValidationStatus.IsIn(CacheValidationStatus.GetIfModifiedSince, CacheValidationStatus.GetIfNoneMatch))) // can only do GET validation, PUT is already impacted backend stores
            {
                cacheValidated = ApplyCacheValidation(tet, cacheValidationStatus, context);
                if (cacheValidated ?? false)
                    return;
            }

            var isResponseCacheable = _validator.IsCacheable(context.HttpContext.Response);
            if (!isRequestCacheable || !isResponseCacheable)
            {
                if (!execCtx.Canceled)
                    context.HttpContext.Response.MakeNonCacheable();
            }

            if (isResponseCacheable)
            {
                context.HttpContext.Response.Headers[HttpHeaderNames.CacheControl] =
                    CacheDirectiveProvider.Get(context.HttpContext).ToString();
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected virtual TimedEntityTagHeaderValue GetTimedETagFromResult(ResourceExecutedContext context, object result)
        {
            var t = typeof(ITimedETagExtractor<>);
            var gt = t.MakeGenericType(result.GetType());
            var tetp = (ITimedETagExtractor)context.HttpContext.RequestServices.GetService(gt);
            if (tetp != null)
            {
                var tet = tetp.Extract(result);
                return tet;
            }

            var cr = result as ICacheResource;
            if (cr != null)
                return cr.GetTimedETag();

            var buffer = _serialiser.Serialise(result);
            using (var hasher = context.HttpContext.RequestServices.GetService<IHasher>())
            {
                return new TimedEntityTagHeaderValue(hasher.ComputeHash(buffer));
            }
        }

        /// <summary>
        /// Whether in addition to sending cache directive for cacheable resources, it should send such directives for non-cachable resources
        /// </summary>
        public bool ApplyNoCacheNoStoreForNonCacheableResponse { get; set; }

        public ICacheDirectiveProvider CacheDirectiveProvider { get; set; }
    }

    /// <summary>
    /// Generic variant of HttpCacheFilter
    /// </summary>
    /// <typeparam name="T">View Model Type</typeparam>
    public class HttpCacheFilter<T> : HttpCacheFilter
    {
        private ICacheabilityValidator _validator;
        private readonly ISerialiser _serialiser;
        private readonly ICacheDirectiveProvider _cacheDirectiveProvider;
        private readonly ITimedETagExtractor _timedETagExtractor;
        private readonly ITimedETagQueryProvider _timedETagQueryProvider;

        public HttpCacheFilter(ICacheabilityValidator validator,
            ISerialiser serialiser,
            ICacheDirectiveProvider<T> cacheDirectiveProvider,
            ITimedETagExtractor<T> timedETagExtractor,
            ITimedETagQueryProvider<T> timedETagQueryProvider) :
            base(validator, serialiser, cacheDirectiveProvider, timedETagExtractor, timedETagQueryProvider)
        {

        }
    }
}