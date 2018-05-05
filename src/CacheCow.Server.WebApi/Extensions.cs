using CacheCow.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace CacheCow.Server.WebApi
{
    internal static class Extensions
    {
        public static CacheValidationStatus GetCacheValidationStatus(this HttpRequestMessage request)
        {
            if (HttpMethod.Get == request.Method)
            {
                if (request.Headers.IfModifiedSince.HasValue)
                    return CacheValidationStatus.GetIfModifiedSince;
                if (request.Headers.IfNoneMatch != null && request.Headers.IfNoneMatch.Count > 0)
                    return CacheValidationStatus.GetIfNoneMatch;
            }

            if (HttpMethod.Post == request.Method)
            {
                if (request.Headers.IfUnmodifiedSince.HasValue)
                    return CacheValidationStatus.PutIfUnModifiedSince;
                if (request.Headers.IfMatch != null && request.Headers.IfMatch.Count > 0)
                    return CacheValidationStatus.PutIfMatch;
            }

            return CacheValidationStatus.None;
        }

        /// <summary>
        /// Makes a response non-cacheable by all the means available to mankind including nuclear
        /// </summary>
        /// <param name="response"></param>
        public static void MakeNonCacheable(this HttpResponseMessage response)
        {
            response.Headers.Pragma.Add(new NameValueHeaderValue("no-cache"));
            response.Headers.CacheControl = new CacheControlHeaderValue()
            {
                NoCache = true,
                NoStore = true
            };

            response.Content.Headers.Expires = DateTimeOffset.Now.AddDays(-1);
        }

        public static void ApplyTimedETag(this HttpResponseMessage response, TimedEntityTagHeaderValue timedETag)
        {
            if (timedETag.LastModified.HasValue)
            {
                response.Content.Headers.LastModified = timedETag.LastModified.Value;
            }
            else if (timedETag.ETag != null)
            {
                response.Headers.ETag = timedETag.ETag;
            }
        }
    }
}