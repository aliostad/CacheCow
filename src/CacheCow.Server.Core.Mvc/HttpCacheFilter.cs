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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CacheCow.Server.Core.Mvc
{
    /// <summary>
    /// A resource filter responsibility for implementing HTTP Caching
    /// Use it with HttpCacheFactoryAttribute
    /// </summary>
    public class HttpCacheFilter : IAsyncResourceFilter
    {
        private ICacheabilityValidator _validator;
        private readonly HttpCachingOptions _options;
        private const string StreamName = "##__travesty_that_I_have_to_do_this__##";

        public HttpCacheFilter(ICacheabilityValidator validator,
            ICacheDirectiveProvider cacheDirectiveProvider, IOptions<HttpCachingOptions> options)
        {
            _validator = validator;

            CacheDirectiveProvider = cacheDirectiveProvider;
            ApplyNoCacheNoStoreForNonCacheableResponse = true;
            _options = options.Value;
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var pipa = new CachingPipeline(_validator, CacheDirectiveProvider, _options)
            {
                ApplyNoCacheNoStoreForNonCacheableResponse = ApplyNoCacheNoStoreForNonCacheableResponse,
                ConfiguredExpiry = ConfiguredExpiry
            };

            var carryon = await pipa.Before(context.HttpContext);

            if (!carryon) // short-circuit
                return;

            var execCtx = await next(); // _______________________________________________________________________________
            var or = execCtx.Result as ObjectResult;
            await pipa.After(context.HttpContext, or == null || or.Value == null ? null : or.Value);
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
            ICacheDirectiveProvider<T> cacheDirectiveProvider,
            IOptions<HttpCachingOptions> options) :
            base(validator, cacheDirectiveProvider, options)
        {
        }
    }
}
