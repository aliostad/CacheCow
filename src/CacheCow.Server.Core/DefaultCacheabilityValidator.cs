using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using CacheCow.Common.Helpers;

namespace CacheCow.Server.Core
{
    /// <summary>
    /// Default implementation according to typical use cases
    /// </summary>
    public class DefaultCacheabilityValidator : ICacheabilityValidator
    {
        public bool IsCacheable(HttpRequest request)
        {
            // none-GET (HEAD is ignored here)
            if (!request.Method.Equals("GET", StringComparison.InvariantCultureIgnoreCase))
                return false;

            // auth
            if (request.Headers.ContainsKey("Authorization"))
                return false;

            // pragma no-cache
            if (request.Headers.ContainsKey("Pragma") &&
                request.Headers["Pragma"].Any(x => x.Contains("no-cache")))
                return false;

            if (request.Headers.ContainsKey("Cache-Control") &&
                request.Headers["Cache-Control"].Any(x => x.Contains("no-cache")))
                return false;

            return true;
        }

        public bool IsCacheable(HttpResponse response)
        {
            if (!response.StatusCode.IsIn(200, 201, 202, 301)) // cacheable statuses
                return false;

            return true;
        }
    }
}
