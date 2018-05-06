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
using System.IO;
using Microsoft.Net.Http.Headers;
using CacheCow.Server.Headers;

namespace CacheCow.Server.Core.Mvc
{
    /// <summary>
    /// A resource filter responsibility for implementing HTTP Caching
    /// Use it with HttpCacheFactoryAttribute
    /// </summary>
    public class HttpCacheFilter : IAsyncResourceFilter
    {
        private ICacheabilityValidator _validator;

        private const string StreamName = "##__travesty_that_I_have_to_do_this__##";

        public HttpCacheFilter(ICacheabilityValidator validator,
            ICacheDirectiveProvider cacheDirectiveProvider)
        {
            _validator = validator;
            CacheDirectiveProvider = cacheDirectiveProvider;
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
                            context.HttpContext.Response.StatusCode = StatusCodes.Status304NotModified;
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
                            context.HttpContext.Response.StatusCode = StatusCodes.Status304NotModified;
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
                            context.Result = new StatusCodeResult(StatusCodes.Status412PreconditionFailed);
                            context.HttpContext.Response.StatusCode = StatusCodes.Status412PreconditionFailed;
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
                            context.Result = new StatusCodeResult(StatusCodes.Status412PreconditionFailed);
                            context.HttpContext.Response.StatusCode = StatusCodes.Status412PreconditionFailed;
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
            var cacheCowHeader = new CacheCowHeader();
            bool? cacheValidated = null;
            bool isRequestCacheable = _validator.IsCacheable(context.HttpContext.Request);
            var cacheValidationStatus = context.HttpContext.Request.GetCacheValidationStatus();
            if (cacheValidationStatus != CacheValidationStatus.None)
            {
                var timedETag = await CacheDirectiveProvider.QueryAsync(context);
                cacheCowHeader.QueryMadeAndSuccessful = timedETag != null;
                cacheValidated = ApplyCacheValidation(timedETag, cacheValidationStatus, context);
                cacheCowHeader.ValidationApplied = true;
                if (cacheValidated ?? false)
                {
                    cacheCowHeader.ShortCircuited = true;
                    cacheCowHeader.ValidationMatched = true;
                    context.HttpContext.Response.Headers.Add(CacheCowHeader.Name, cacheCowHeader.ToString());
                    // the response would have been set and no need to run the rest of the pipeline
                    return;
                }
            }

            context.HttpContext.Items[StreamName] = context.HttpContext.Response.Body;
            context.HttpContext.Response.Body = new MemoryStream();

            var execCtx = await next(); // _______________________________________________________________________________

            var ms = context.HttpContext.Response.Body as MemoryStream;
            bool mustReflush = ms != null && ms.Length > 0;
            context.HttpContext.Response.Body = (Stream) context.HttpContext.Items[StreamName];

            try
            {
                if (HttpMethods.IsGet(context.HttpContext.Request.Method))
                {
                    context.HttpContext.Response.Headers.Add(HeaderNames.Vary, string.Join(";", CacheDirectiveProvider.GetVaryHeaders(context.HttpContext)));
                    var cacheControl = CacheDirectiveProvider.GetCacheControl(context.HttpContext, this.ConfiguredExpiry);
                    var isResponseCacheable = _validator.IsCacheable(context.HttpContext.Response);
                    if (!cacheControl.NoStore && isResponseCacheable) // _______ is cacheable
                    {
                        var or = execCtx.Result as ObjectResult;
                        TimedEntityTagHeaderValue tet = null;
                        if (or != null && or.Value != null)
                        {
                            tet = CacheDirectiveProvider.Extract(or.Value);
                        }

                        if (cacheValidated == null  // could not validate
                            && tet != null
                            && cacheValidationStatus != CacheValidationStatus.None) // can only do GET validation, PUT is already impacted backend stores
                        {
                            cacheValidated = ApplyCacheValidation(tet, cacheValidationStatus, context);
                            cacheCowHeader.ValidationApplied = true;
                            // the response would have been set and no need to run the rest of the pipeline

                            if (cacheValidated ?? false)
                            {
                                cacheCowHeader.ValidationMatched = true;
                                context.HttpContext.Response.Headers.Add(CacheCowHeader.Name, cacheCowHeader.ToString());
                                return;
                            }
                        }

                        if (tet != null)
                            context.HttpContext.Response.ApplyTimedETag(tet);
                    }

                    if (!isRequestCacheable || !isResponseCacheable)
                        context.HttpContext.Response.MakeNonCacheable();
                    else
                        context.HttpContext.Response.Headers[HttpHeaderNames.CacheControl] = cacheControl.ToString();

                    context.HttpContext.Response.Headers.Add(CacheCowHeader.Name, cacheCowHeader.ToString());
                }

            }
            finally
            {
                if (mustReflush)
                {
                    ms.Position = 0;
                    ms.CopyTo(context.HttpContext.Response.Body);
                }
            }
        }

        /// <summary>
        /// Whether in addition to sending cache directive for cacheable resources, it should send such directives for non-cachable resources
        /// </summary>
        public bool ApplyNoCacheNoStoreForNonCacheableResponse { get; set; }

        /// <summary>
        /// ICacheDirectiveProvider for this instance
        /// </summary>
        public ICacheDirectiveProvider CacheDirectiveProvider { get; set; }

        /// <summary>
        /// Gets used to create Cache directives
        /// </summary>
        public TimeSpan? ConfiguredExpiry { get; set; }

    }

    /// <summary>
    /// Generic variant of HttpCacheFilter
    /// </summary>
    /// <typeparam name="T">View Model Type</typeparam>
    public class HttpCacheFilter<T> : HttpCacheFilter
    {
        public HttpCacheFilter(ICacheabilityValidator validator,
            ICacheDirectiveProvider<T> cacheDirectiveProvider) :
            base(validator, cacheDirectiveProvider)
        {
        }
    }
}