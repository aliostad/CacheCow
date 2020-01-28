using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;
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
        private IConfiguration _config;
        private const string StreamName = "##__travesty_that_I_have_to_do_this__##";

        public HttpCacheFilter(ICacheabilityValidator validator,
            ICacheDirectiveProvider cacheDirectiveProvider,
            IOptions<HttpCachingOptions> options,
            IConfiguration configuration)
        {
            _validator = validator;

            CacheDirectiveProvider = cacheDirectiveProvider;
            ApplyNoCacheNoStoreForNonCacheableResponse = true;
            _options = options.Value;
            _config = configuration;
        }

        private HttpCacheSettings GetConfigSettings(ResourceExecutingContext context, HttpCacheSettings settings)
        {
            const string ControllerKey = "controller";
            const string ActionKey = "action";
            if (!context.RouteData.Values.ContainsKey(ControllerKey) ||
                !context.RouteData.Values.ContainsKey(ActionKey))
                return settings;

            var key = $"CacheCow:{context.RouteData.Values[ControllerKey]}:{context.RouteData.Values[ActionKey]}";
            var section = _config.GetSection(key);
            if (section.Exists())
                section.Bind(settings);
            return settings;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var settings = new HttpCacheSettings()
            {
                Expiry = ConfiguredExpiry
            };

            if (_options.EnableConfiguration)
                settings = GetConfigSettings(context, settings);

            if (!settings.Enabled)
            {
                await next();
                return;
            }

            var pipa = new CachingPipeline(_validator, CacheDirectiveProvider, _options)
            {
                ApplyNoCacheNoStoreForNonCacheableResponse = ApplyNoCacheNoStoreForNonCacheableResponse,
                ConfiguredExpiry = settings.Expiry
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

        /// <summary>
        /// Whether to look to extract values from configuration
        /// </summary>
        public bool IsConfigurationEnabled { get; set; }
    }

    /// <summary>
    /// Generic variant of HttpCacheFilter
    /// </summary>
    /// <typeparam name="T">View Model Type</typeparam>
    public class HttpCacheFilter<T> : HttpCacheFilter
    {
        public HttpCacheFilter(ICacheabilityValidator validator,
            ICacheDirectiveProvider<T> cacheDirectiveProvider,
            IOptions<HttpCachingOptions> options,
            IConfiguration configuration) :
            base(validator, cacheDirectiveProvider, options, configuration)
        {
        }
    }
}
