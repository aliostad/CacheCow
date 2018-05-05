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

namespace CacheCow.Server.WebApi
{
    /// <summary>
    /// Main impl of server HTTP caching on web api
    /// </summary>
    public class HttpCacheAttribute : ActionFilterAttribute
    {

        public HttpCacheAttribute(Type cacheabilityValidatorType = null,
            Type cacheDirectiveProviderType = null,
            Type timedETagExtractorType = null,
            Type timedETagQueryProviderType = null)
        {

            if(CachingRuntime.Factory == null)
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
                    (ITimedETagQueryProvider) Activator.CreateInstance(timedETagQueryProviderType);

                CacheDirectiveProvider = cacheDirectiveProviderType == null ?
                    new DefaultCacheDirectiveProvider(extractor, queryProvider) :
                    (ICacheDirectiveProvider) Activator.CreateInstance(cacheDirectiveProviderType);
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
            HttpActionContext context)
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


        public override Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            return base.OnActionExecutingAsync(actionContext, cancellationToken);
        }

        public override Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            return base.OnActionExecutedAsync(actionExecutedContext, cancellationToken);
        }

        public ICacheabilityValidator CacheabilityValidator { get; set; }

        public ICacheDirectiveProvider CacheDirectiveProvider { get; set; }


    }
}
