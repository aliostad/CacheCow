using System;
using System.Collections.Generic;
using System.Text;
using CacheCow.Common;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;

namespace CacheCow.Server.Core
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
                if (typedHeaders.IfNoneMatch != null && typedHeaders.IfNoneMatch.Count == 0)
                    return CacheValidationStatus.GetIfNoneMatch;
            }
            
            if(HttpMethods.IsPut(request.Method))
            {
                if (typedHeaders.IfUnmodifiedSince.HasValue)
                    return CacheValidationStatus.PutIfUnModifiedSince;
                if (typedHeaders.IfMatch != null && typedHeaders.IfMatch.Count == 0)
                    return CacheValidationStatus.PutIfMatch;
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
    }
}
