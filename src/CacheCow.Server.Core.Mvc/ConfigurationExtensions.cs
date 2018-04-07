using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using CacheCow.Common;

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
        public static void AddHttpCaching(this IServiceCollection services)
        {
            services.AddTransient<ICacheabilityValidator, DefaultCacheabilityValidator>();
            services.AddTransient<HttpCacheFilter>();
            services.AddTransient<ISerialiser, JsonSerialiser>();
            services.AddTransient<IHasher, Sha1Hasher>();
            services.AddTransient<ITimedETagExtractor, DefaultTimedETagExtractor>();
            services.AddTransient<ITimedETagQueryProvider, NullQueryProvider>();
            services.AddTransient<ICacheDirectiveProvider, DefaultCacheDirectiveProvider>();
        }

        public static void AddDirectiveProviderForViewModel<TViewModel, TCacheDirectiveProvider>(this IServiceCollection services, bool transient = true)
            where TCacheDirectiveProvider : class, ICacheDirectiveProvider<TViewModel>
        {           
            services.AddServiceWithLifeTime<ICacheDirectiveProvider<TViewModel>, TCacheDirectiveProvider>(transient);
            services.AddServiceWithLifeTime<ITimedETagQueryProvider<TViewModel>, NullQueryProvider<TViewModel>>(transient);
            services.AddServiceWithLifeTime<ITimedETagExtractor<TViewModel>, DefaultTimedETagExtractor<TViewModel>>(transient);
            services.AddServiceWithLifeTime<HttpCacheFilter<TViewModel>>(transient);                
        }

        public static void AddSeparateDirectiveAndQueryProviderForViewModel<TViewModel, TCacheDirectiveProvider, TQueryProvider>(this IServiceCollection services, bool transient = true)
            where TCacheDirectiveProvider : class, ICacheDirectiveProvider<TViewModel>
            where TQueryProvider: class, ITimedETagQueryProvider<TViewModel>
        {
            services.AddServiceWithLifeTime<ICacheDirectiveProvider<TViewModel>, TCacheDirectiveProvider>(transient);
            services.AddServiceWithLifeTime<ITimedETagQueryProvider<TViewModel>, TQueryProvider>(transient);
            services.AddServiceWithLifeTime<ITimedETagExtractor<TViewModel>, DefaultTimedETagExtractor<TViewModel>>(transient);
            services.AddServiceWithLifeTime<HttpCacheFilter<TViewModel>>(transient);
        }

        public static void AddQueryProviderForViewModel<TViewModel, TQueryProvider>(this IServiceCollection services, bool transient = true)
            where TQueryProvider : class, ITimedETagQueryProvider<TViewModel>
        {
            services.AddServiceWithLifeTime<ICacheDirectiveProvider<TViewModel>, DefaultCacheDirectiveProvider<TViewModel>>(transient);
            services.AddServiceWithLifeTime<ITimedETagQueryProvider<TViewModel>, TQueryProvider>(transient);
            services.AddServiceWithLifeTime<ITimedETagExtractor<TViewModel>, DefaultTimedETagExtractor<TViewModel>>(transient);
            services.AddServiceWithLifeTime<HttpCacheFilter<TViewModel>>(transient);
        }

        public static void AddExtractorForViewModel<TViewModel, TExtractor>(this IServiceCollection services, bool transient = true)
            where TExtractor : class, ITimedETagExtractor<TViewModel>
        {
            services.AddServiceWithLifeTime<ICacheDirectiveProvider<TViewModel>, DefaultCacheDirectiveProvider<TViewModel>>(transient);
            services.AddServiceWithLifeTime<ITimedETagQueryProvider<TViewModel>, NullQueryProvider<TViewModel>>(transient);
            services.AddServiceWithLifeTime<ITimedETagExtractor<TViewModel>, DefaultTimedETagExtractor<TViewModel>>(transient);
            services.AddServiceWithLifeTime<HttpCacheFilter<TViewModel>>(transient);
        }

        private static void AddServiceWithLifeTime<TService, TImplementation>(this IServiceCollection services, bool transient)
            where TImplementation : class, TService
            where TService : class
        {
            if (transient)
                services.AddTransient<TService, TImplementation>();
            else
                services.AddSingleton<TService, TImplementation>();
        }

        private static void AddServiceWithLifeTime<TService>(this IServiceCollection services, bool transient)
            where TService : class
        {
            if (transient)
                services.AddTransient<TService>();
            else
                services.AddSingleton<TService>();
        }

        internal static T GetService<T>(this IServiceProvider provider)
        {
            return (T) provider.GetService(typeof(T));
        }
    }
}
