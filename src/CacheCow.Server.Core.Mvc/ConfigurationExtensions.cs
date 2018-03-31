using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server.Core.Mvc
{
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
        }

        internal static T GetService<T>(this IServiceProvider provider)
        {
            return (T) provider.GetService(typeof(T));
        }
    }
}
