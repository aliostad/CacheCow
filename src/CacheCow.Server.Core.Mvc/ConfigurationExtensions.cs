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
            services.AddTransient<StrongConsistencyProvider>();
            services.AddTransient<ConstantExpiryProvider>();
            services.AddTransient<ISerialiser, JsonSerialiser>();
            services.AddTransient<IHasher, Sha1Hasher>();
            services.AddTransient<ICacheDirectiveProvider, NoCacheNoStoreProvider>();
            services.AddTransient<ITimedETagExtractor, DefaultTimedETagExtractor>();
            services.AddTransient<ITimedETagQueryProvider, NullQueryProvider>();
        }
        /*
        public static void AddHttpCachingForViewModel<TViewModel, TETagExtractor>(this IServiceCollection services, bool transient = true)
            where TETagExtractor : class, ITimedETagExtractor<TViewModel>
        {
//            ICacheDirectiveProvider<T> cacheDirectiveProvider,
//ITimedETagExtractor< T > timedETagExtractor,
//            ITimedETagQueryProvider<T> timedETagQueryProvider) :

           if (transient)
            {
                services.AddTransient<ITimedETagExtractor<TViewModel>, TETagExtractor>();
                services.AddTransient<HttpCacheFilter<TViewModel>>();
                services.AddTransient<HttpCacheFilter<TViewModel>>();
                services.AddTransient<HttpCacheFilter<TViewModel>>();
            }
           else
            {

            }
        }
        */

        private static void AddServiceConditional<TService, TImplementation>(this IServiceCollection services, bool transient)
            where TImplementation : class, TService
            where TService : class
        {
            if (transient)
                services.AddTransient<TService, TImplementation>();
            else
                services.AddSingleton<TService, TImplementation>();
        }


        internal static T GetService<T>(this IServiceProvider provider)
        {
            return (T) provider.GetService(typeof(T));
        }
    }
}
