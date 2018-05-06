using CacheCow.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Net;
using System.Net.Http;
using System.Linq;
using CacheCow.Server.Headers;

namespace CacheCow.Server.WebApi
{
    /// <summary>
    /// Main impl of server HTTP caching on web api
    /// </summary>
    public class HttpCacheAttribute : ActionFilterAttribute
    {
        private const string CacheValidatedKey = "###__cache_validated__###";
        private const string CacheCowHeaderKey = "###__cachecow_header__###";

        public HttpCacheAttribute(Type cacheabilityValidatorType = null,
            Type cacheDirectiveProviderType = null,
            Type timedETagExtractorType = null,
            Type timedETagQueryProviderType = null)
        {

            if (CachingRuntime.Factory != null)
            {
                CacheabilityValidator = CachingRuntime.Get<ICacheabilityValidator>();
                CacheDirectiveProvider = CachingRuntime.Get<ICacheDirectiveProvider>();
            }
            else
            {
                CacheabilityValidator = cacheabilityValidatorType == null ?
                    new DefaultCacheabilityValidator() :
                    (ICacheabilityValidator)Activator.CreateInstance(cacheabilityValidatorType);

                var extractor = timedETagExtractorType == null ?
                    new DefaultTimedETagExtractor(new JsonSerialiser(), new Sha1Hasher()) :
                    (ITimedETagExtractor)Activator.CreateInstance(timedETagExtractorType);

                var queryProvider = timedETagQueryProviderType == null ?
                    new NullQueryProvider() :
                    (ITimedETagQueryProvider)Activator.CreateInstance(timedETagQueryProviderType);

                CacheDirectiveProvider = cacheDirectiveProviderType == null ?
                    new DefaultCacheDirectiveProvider(extractor, queryProvider) :
                    (ICacheDirectiveProvider)Activator.CreateInstance(cacheDirectiveProviderType);
            }

            CachingRuntime.OnHttpCacheCreated(new HttpCacheCreatedEventArgs(this));
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
            ContextUnifier context)
        {
            if (timedEtag == null)
                return null;

            switch (cacheValidationStatus)
            {
                case CacheValidationStatus.GetIfModifiedSince:
                    if (timedEtag.LastModified == null)
                        return false;
                    else
                    {
                        if (timedEtag.LastModified > context.Request.Headers.IfModifiedSince.Value)
                            return false;
                        else
                        {
                            context.Response = new HttpResponseMessage(HttpStatusCode.NotModified);
                            return true;
                        }
                    }

                case CacheValidationStatus.GetIfNoneMatch:
                    if (timedEtag.ETag == null)
                        return false;
                    else
                    {
                        if (context.Request.Headers.IfNoneMatch.Any(x => x.Tag == timedEtag.ETag.Tag))
                        {
                            context.Response = new HttpResponseMessage(HttpStatusCode.NotModified);
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
                        if (context.Request.Headers.IfMatch.Any(x => x.Tag == timedEtag.ETag.Tag))
                            return false;
                        else
                        {
                            context.Response = new HttpResponseMessage(HttpStatusCode.PreconditionFailed);
                            return true;
                        }
                    }
                case CacheValidationStatus.PutIfUnModifiedSince:
                    if (timedEtag.LastModified == null)
                        return false;
                    else
                    {
                        if (timedEtag.LastModified > context.Request.Headers.IfUnmodifiedSince.Value)
                        {
                            context.Response = new HttpResponseMessage(HttpStatusCode.PreconditionFailed);
                            return true;
                        }
                        else
                            return false;
                    }

                default:
                    return null;
            }
        }


        public async override Task OnActionExecutingAsync(HttpActionContext context, CancellationToken cancellationToken)
        {
            await base.OnActionExecutingAsync(context, cancellationToken);

            var cacheCowHeader = new CacheCowHeader();
            context.Request.Properties[CacheCowHeaderKey] = cacheCowHeader;
            bool? cacheValidated = null;
            bool isRequestCacheable = CacheabilityValidator.IsCacheable(context.Request);
            var cacheValidationStatus = context.Request.GetCacheValidationStatus();
            if (cacheValidationStatus != CacheValidationStatus.None)
            {
                var timedETag = await CacheDirectiveProvider.QueryAsync(context);
                cacheCowHeader.QueryMadeAndSuccessful = timedETag != null;
                cacheValidated = ApplyCacheValidation(timedETag, cacheValidationStatus, new ContextUnifier(context));
                context.Request.Properties.Add(CacheValidatedKey, cacheValidated);
                cacheCowHeader.ValidationApplied = true;
                if (cacheValidated ?? false)
                {
                    cacheCowHeader.ShortCircuited = true;
                    cacheCowHeader.ValidationMatched = true;
                    context.Response.Headers.Add(CacheCowHeader.Name, cacheCowHeader.ToString());
                    // the response would have been set and no need to run the rest of the pipeline
                    return;
                }
            }
        }

        public async override Task OnActionExecutedAsync(HttpActionExecutedContext context, CancellationToken cancellationToken)
        {
            await base.OnActionExecutedAsync(context, cancellationToken);
            bool? cacheValidated = context.Request.Properties.ContainsKey(CacheValidatedKey) ?
                (bool?) context.Request.Properties[CacheValidatedKey] : null;
            var cacheValidationStatus = context.Request.GetCacheValidationStatus();
            var cacheCowHeader = (CacheCowHeader) context.Request.Properties[CacheCowHeaderKey];
            bool isRequestCacheable = CacheabilityValidator.IsCacheable(context.Request);

            if (HttpMethod.Get == context.Request.Method)
            {
                context.Response.Headers.Add("Vary", string.Join(";", CacheDirectiveProvider.GetVaryHeaders(context)));
                var cacheControl = CacheDirectiveProvider.GetCacheControl(context, TimeSpan.FromSeconds(this.DefaultExpirySeconds));
                var isResponseCacheable = CacheabilityValidator.IsCacheable(context.Response);
                if (!cacheControl.NoStore && isResponseCacheable) // _______ is cacheable
                {
                    var or = context.Response.Content as ObjectContent;
                    TimedEntityTagHeaderValue tet = null;
                    if (or != null && or.Value != null)
                    {
                        tet = CacheDirectiveProvider.Extract(or.Value);
                    }

                    if (cacheValidated == null  // could not validate
                        && tet != null
                        && cacheValidationStatus != CacheValidationStatus.None) // can only do GET validation, PUT is already impacted backend stores
                    {
                        cacheValidated = ApplyCacheValidation(tet, cacheValidationStatus, new ContextUnifier(context));
                        cacheCowHeader.ValidationApplied = true;
                        // the response would have been set and no need to run the rest of the pipeline

                        if (cacheValidated ?? false)
                        {
                            cacheCowHeader.ValidationMatched = true;
                            context.Response.Headers.Add(CacheCowHeader.Name, cacheCowHeader.ToString());
                            return;
                        }
                    }

                    if (tet != null)
                        context.Response.ApplyTimedETag(tet);
                }

                if (!isRequestCacheable || !isResponseCacheable)
                    context.Response.MakeNonCacheable();
                else
                    context.Response.Headers.Add(HttpHeaderNames.CacheControl, cacheControl.ToString());

                context.Response.Headers.Add(CacheCowHeader.Name, cacheCowHeader.ToString());
            }


        }

        public ICacheabilityValidator CacheabilityValidator { get; set; }

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
        public int DefaultExpirySeconds { get; set; }


    }
}
