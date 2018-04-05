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
            if (request.Headers.Any(x => x.Key.Equals("Authorization", StringComparison.InvariantCultureIgnoreCase)))
                return false;

            // pragma no-cache
            if (request.Headers.Any(x => x.Key.Equals("Pragma", StringComparison.InvariantCultureIgnoreCase)) &&
                request.Headers["Pragma"].Any(x => x.Contains("no-cache")))
                return false;

            if (request.Headers.Any(x => x.Key.Equals("Cache-Control", StringComparison.InvariantCultureIgnoreCase)) &&
                request.Headers["Cache-Control"].Any(x => x.Contains("no-cache")))
                return false;

            return true;
        }

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
    }
}
