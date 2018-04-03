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
            services.AddTransient<ConstantExpiryProvider>();
            services.AddTransient<StrongConsistencyProvider>();
            services.AddTransient<ISerialiser, JsonSerialiser>();
            services.AddTransient<IHasher, Sha1Hasher>();
            services.AddTransient<ICacheDirectiveProvider, NoCacheNoStoreProvider>();
            services.AddTransient<ITimedETagExtractor, DefaultTimedETagExtractor>();
            services.AddTransient<ITimedETagQueryProvider, NullQueryProvider>();
        }

        internal static T GetService<T>(this IServiceProvider provider)
        {
            return (T) provider.GetService(typeof(T));
        }
    }
}
