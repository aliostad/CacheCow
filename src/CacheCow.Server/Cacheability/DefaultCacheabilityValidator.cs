using System;
using System.Collections.Generic;
using System.Linq;
#if NET452
using System.Net.Http;
using System.Net;
#else
using Microsoft.AspNetCore.Http;
#endif
using CacheCow.Common.Helpers;

namespace CacheCow.Server
{
    /// <summary>
    /// Default implementation according to typical use cases
    /// </summary>
    public class DefaultCacheabilityValidator : ICacheabilityValidator
    {
#if NET452
        public bool IsCacheable(HttpRequestMessage request)
        {
            // none-GET (HEAD is ignored here!! Need to change later!!)
            if (request.Method != HttpMethod.Get)
                return false;

            // auth
            if (request.Headers.Any(x => x.Key.Equals("Authorization", StringComparison.InvariantCultureIgnoreCase)))
                return false;

            // pragma no-cache

            if (request.Headers.Pragma != null && request.Headers.Pragma.Any(x => x.Name.Equals("no-cache", StringComparison.InvariantCultureIgnoreCase)))
                return false;

            if (request.Headers.CacheControl != null && request.Headers.CacheControl.NoStore)
                return false;

            return true;
        }
#else
        public bool IsCacheable(HttpRequest request)
       {
            // none-GET (HEAD is ignored here)
            if (!request.Method.Equals("GET", StringComparison.InvariantCultureIgnoreCase))
                return false;

            // auth
            if (request.Headers.Any(x => x.Key.Equals("Authorization", StringComparison.InvariantCultureIgnoreCase)))
                return false;

            // pragma no-cache
            if (request.Headers.Any(x => x.Key.Equals("Pragma", StringComparison.InvariantCultureIgnoreCase)) &&
                request.Headers["Pragma"].Any(x => x.Contains("no-cache")))
                return false;

            if (request.Headers.Any(x => x.Key.Equals("Cache-Control", StringComparison.InvariantCultureIgnoreCase)) &&
                request.Headers["Cache-Control"].Any(x => x.Contains("no-store")))
                return false;

            return true;
        }
#endif

#if NET452
        public bool IsCacheable(HttpResponseMessage response)
        {
            // cacheable statuses
            if (!response.StatusCode.IsIn(HttpStatusCode.OK, 
                HttpStatusCode.Created,
                HttpStatusCode.Accepted, 
                HttpStatusCode.MovedPermanently,
                HttpStatusCode.NotModified))
                return false;

            // cookie
            if (response.Headers.Any(x => x.Key.Equals("set-cookie", StringComparison.InvariantCultureIgnoreCase)))
                return false;

            return true;
        }
#else
        public bool IsCacheable(HttpResponse response)
        {
            // cacheable statuses
            if (!response.StatusCode.IsIn(200, 201, 202, 301, 304))
                return false;

            // cookie
            if (response.Headers.Any(x => x.Key.Equals("set-cookie", StringComparison.InvariantCultureIgnoreCase)))
                return false;

            return true;
        }
#endif

    }
}
