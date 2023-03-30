#if NET462
#else
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CacheCow.Common;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("CacheCow.Server.Core.Mvc")]

namespace CacheCow.Server
{
    /// <summary>
    /// Extensions for ASP.NET Core
    /// </summary>
    public static class CoreExtensions
    {
        private const string RequestHeadersKey = "###__request_headers__###";
        private const string ResponseHeadersKey = "###__response_headers__###";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Cache Validation Status</returns>
        public static CacheValidationStatus GetCacheValidationStatus(this HttpRequest request)
        {
            var typedHeaders = request.GetTypedHeadersWithCaching();
            if (HttpMethods.IsGet(request.Method))
            {
                if (typedHeaders.IfModifiedSince.HasValue)
                    return CacheValidationStatus.GetIfModifiedSince;
                if (typedHeaders.IfNoneMatch != null && typedHeaders.IfNoneMatch.Count > 0)
                    return CacheValidationStatus.GetIfNoneMatch;
            }
            
            if(HttpMethods.IsPut(request.Method) || HttpMethods.IsDelete(request.Method) || HttpMethods.IsPatch(request.Method))
            {
                if (typedHeaders.IfUnmodifiedSince.HasValue)
                    return CacheValidationStatus.PutPatchDeleteIfUnModifiedSince;
                if (typedHeaders.IfMatch != null && typedHeaders.IfMatch.Count > 0)
                    return CacheValidationStatus.PutPatchDeleteIfMatch;
            }

            return CacheValidationStatus.None;
        }

        /// <summary>
        /// Should have been part of the framework. Bear in mind, it is taken at the start so if you make any changes to request you won't see it.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static RequestHeaders GetTypedHeadersWithCaching(this HttpRequest request)
        {
            if (!request.HttpContext.Items.ContainsKey(RequestHeadersKey))
                request.HttpContext.Items[RequestHeadersKey] = request.GetTypedHeaders();

            return (RequestHeaders)request.HttpContext.Items[RequestHeadersKey];
        }

         /// <summary>
        /// Should have been part of the framework. 
        /// Bear in mind, it is taken first time it is asked so if you make any changes to the response you won't see it.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static RequestHeaders GetTypedHeadersWithCaching(this HttpResponse response)
        {
            if (!response.HttpContext.Items.ContainsKey(ResponseHeadersKey))
                response.HttpContext.Items[ResponseHeadersKey] = response.GetTypedHeaders();

            return (RequestHeaders)response.HttpContext.Items[ResponseHeadersKey];
        }

        /// <summary>
        /// Makes a response non-cacheable by all the means available to mankind including nuclear
        /// </summary>
        /// <param name="response"></param>
        public static void MakeNonCacheable(this HttpResponse response)
        {
            response.Headers[HttpHeaderNames.Pragma] = "no-cache";
            response.Headers[HttpHeaderNames.CacheControl] = "no-cache;no-store";
            response.Headers[HttpHeaderNames.Expires] = "-1";
        }

        public static void ApplyTimedETag(this HttpResponse response, TimedEntityTagHeaderValue timedETag)
        {
            if(timedETag.LastModified.HasValue)
            {
                response.Headers[HttpHeaderNames.LastModified] = timedETag.LastModified.Value.ToUniversalTime().ToString("r");
            }
            else if(timedETag.ETag != null)
            {
                response.Headers[HttpHeaderNames.ETag] = timedETag.ETag.ToString();
            }
        }

        /// <summary>
        /// Adds default implementation of various interfaces
        /// </summary>
        /// <param name="services">services</param>
        public static void AddHttpCaching(this IServiceCollection services)
        {
            services.AddTransient<ICacheabilityValidator, DefaultCacheabilityValidator>();
            services.AddTransient<ISerialiser, JsonSerialiser>();
            services.AddTransient<IHasher, Sha1Hasher>();
            services.AddTransient<ITimedETagExtractor, DefaultTimedETagExtractor>();
            services.AddTransient<ITimedETagQueryProvider, NullQueryProvider>();
            services.AddTransient<ICacheDirectiveProvider, DefaultCacheDirectiveProvider>();
            services.AddTransient<ICachingPipeline, CachingPipeline>();
        }

       
        public static IServiceCollection Remove<T>(this IServiceCollection services)
        {
            var serviceDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(T));
            if (serviceDescriptor != null)
                services.Remove(serviceDescriptor);

            return services;
        }

        public static void AddDirectiveProviderForViewModel<TViewModel, TCacheDirectiveProvider>(this IServiceCollection services, bool transient = true)
            where TCacheDirectiveProvider : class, ICacheDirectiveProvider<TViewModel>
        {
            services.AddServiceWithLifeTime<ICacheDirectiveProvider<TViewModel>, TCacheDirectiveProvider>(transient);
            services.AddServiceWithLifeTime<ITimedETagQueryProvider<TViewModel>, NullQueryProvider<TViewModel>>(transient);
            services.AddServiceWithLifeTime<ITimedETagExtractor<TViewModel>, DefaultTimedETagExtractor<TViewModel>>(transient);
            services.AddTransient<ICachingPipeline<TViewModel>, CachingPipeline<TViewModel>>();
        }

        public static void AddSeparateDirectiveAndQueryProviderForViewModel<TViewModel, TCacheDirectiveProvider, TQueryProvider>(this IServiceCollection services, bool transient = true)
            where TCacheDirectiveProvider : class, ICacheDirectiveProvider<TViewModel>
            where TQueryProvider : class, ITimedETagQueryProvider<TViewModel>
        {
            services.AddServiceWithLifeTime<ICacheDirectiveProvider<TViewModel>, TCacheDirectiveProvider>(transient);
            services.AddServiceWithLifeTime<ITimedETagQueryProvider<TViewModel>, TQueryProvider>(transient);
            services.AddServiceWithLifeTime<ITimedETagExtractor<TViewModel>, DefaultTimedETagExtractor<TViewModel>>(transient);
            services.AddTransient<ICachingPipeline<TViewModel>, CachingPipeline<TViewModel>>();
        }

        public static void AddQueryProviderForViewModel<TViewModel, TQueryProvider>(this IServiceCollection services, bool transient = true)
            where TQueryProvider : class, ITimedETagQueryProvider<TViewModel>
        {
            services.AddServiceWithLifeTime<ICacheDirectiveProvider<TViewModel>, DefaultCacheDirectiveProvider<TViewModel>>(transient);
            services.AddServiceWithLifeTime<ITimedETagQueryProvider<TViewModel>, TQueryProvider>(transient);
            services.AddServiceWithLifeTime<ITimedETagExtractor<TViewModel>, DefaultTimedETagExtractor<TViewModel>>(transient);
            services.AddTransient<ICachingPipeline<TViewModel>, CachingPipeline<TViewModel>>();
        }

        public static void AddQueryProviderAndExtractorForViewModel<TViewModel, TQueryProvider, TExtractor>(this IServiceCollection services, bool transient = true)
            where TQueryProvider : class, ITimedETagQueryProvider<TViewModel>
            where TExtractor : class, ITimedETagExtractor<TViewModel>
        {
            services.AddServiceWithLifeTime<ICacheDirectiveProvider<TViewModel>, DefaultCacheDirectiveProvider<TViewModel>>(transient);
            services.AddServiceWithLifeTime<ITimedETagQueryProvider<TViewModel>, TQueryProvider>(transient);
            services.AddServiceWithLifeTime<ITimedETagExtractor<TViewModel>, TExtractor>(transient);
            services.AddTransient<ICachingPipeline<TViewModel>, CachingPipeline<TViewModel>>();
        }


        public static void AddExtractorForViewModel<TViewModel, TExtractor>(this IServiceCollection services, bool transient = true)
            where TExtractor : class, ITimedETagExtractor<TViewModel>
        {
            services.AddServiceWithLifeTime<ICacheDirectiveProvider<TViewModel>, DefaultCacheDirectiveProvider<TViewModel>>(transient);
            services.AddServiceWithLifeTime<ITimedETagQueryProvider<TViewModel>, NullQueryProvider<TViewModel>>(transient);
            services.AddServiceWithLifeTime<ITimedETagExtractor<TViewModel>, TExtractor>(transient);
            services.AddTransient<ICachingPipeline<TViewModel>, CachingPipeline<TViewModel>>();
        }

        internal static void AddServiceWithLifeTime<TService, TImplementation>(this IServiceCollection services, bool transient)
            where TImplementation : class, TService
            where TService : class
        {
            if (transient)
                services.AddTransient<TService, TImplementation>();
            else
                services.AddSingleton<TService, TImplementation>();
        }

        internal static void AddServiceWithLifeTime<TService>(this IServiceCollection services, bool transient)
            where TService : class
        {
            if (transient)
                services.AddTransient<TService>();
            else
                services.AddSingleton<TService>();
        }

        internal static T GetService<T>(this IServiceProvider provider)
        {
            return (T)provider.GetService(typeof(T));
        }

    }
}
#endif