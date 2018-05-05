using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace CacheCow.Server.WebApi
{
    /// <summary>
    /// TODO: this is not done
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
