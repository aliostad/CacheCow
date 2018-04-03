using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace CacheCow.Server.Core
{
    /// <summary>
    /// This class sets expiry to zero so that teh client should always revalidate
    /// </summary>
    public class StrongConsistencyProvider : ICacheDirectiveProvider
    {
        public CacheControlHeaderValue Get(HttpContext context)
        {
            return new CacheControlHeaderValue()
            {
                Private = true,
                MustRevalidate = true,
                MaxAge = TimeSpan.Zero
            };
        }
    }
}
