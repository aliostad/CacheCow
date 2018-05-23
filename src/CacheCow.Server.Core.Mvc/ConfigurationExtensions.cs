using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using CacheCow.Common;
using CacheCow.Server;

namespace CacheCow.Server.Core.Mvc
{
    /// <summary>
    /// MVC Core extensions
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds default implementation of various interfaces
        /// </summary>
        /// <param name="services">services</param>
        public static void AddHttpCachingMvc(this IServiceCollection services)
        {
            services.AddHttpCaching();
            services.AddTransient<HttpCacheFilter>();
        }

        public static void AddDirectiveProviderForViewModelMvc<TViewModel, TCacheDirectiveProvider>(this IServiceCollection services, bool transient = true)
where TCacheDirectiveProvider : class, ICacheDirectiveProvider<TViewModel>
        {
            services.AddDirectiveProviderForViewModel<TViewModel, TCacheDirectiveProvider>(transient);
            services.AddServiceWithLifeTime<HttpCacheFilter<TViewModel>>(transient);
        }

        public static void AddSeparateDirectiveAndQueryProviderForViewModelMvc<TViewModel, TCacheDirectiveProvider, TQueryProvider>(this IServiceCollection services, bool transient = true)
            where TCacheDirectiveProvider : class, ICacheDirectiveProvider<TViewModel>
            where TQueryProvider : class, ITimedETagQueryProvider<TViewModel>
        {
            services.AddSeparateDirectiveAndQueryProviderForViewModel<TViewModel, TCacheDirectiveProvider, TQueryProvider>(transient);
            services.AddServiceWithLifeTime<HttpCacheFilter<TViewModel>>(transient);
        }

        public static void AddQueryProviderForViewModelMvc<TViewModel, TQueryProvider>(this IServiceCollection services, bool transient = true)
            where TQueryProvider : class, ITimedETagQueryProvider<TViewModel>
        {
            services.AddQueryProviderForViewModel<TViewModel, TQueryProvider>(transient);
            services.AddServiceWithLifeTime<HttpCacheFilter<TViewModel>>(transient);
        }

        public static void AddExtractorForViewModelMvc<TViewModel, TExtractor>(this IServiceCollection services, bool transient = true)
            where TExtractor : class, ITimedETagExtractor<TViewModel>
        {
            services.AddExtractorForViewModel<TViewModel, TExtractor>(transient);
            services.AddServiceWithLifeTime<HttpCacheFilter<TViewModel>>(transient);
        }
    }
}
