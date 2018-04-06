using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace CacheCow.Server.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class ConstantExpiryProvider : CacheDirectiveProviderBase
    {
        public ConstantExpiryProvider(ITimedETagExtractor timedETagExtractor) : base(timedETagExtractor)
        {
        }

        public TimeSpan Expiry { get; set; }

        public bool IsPublic { get; set; }

        public void Dispose()
        {
            // nilch
        }

        public override CacheControlHeaderValue Get(HttpContext context)
        {
            return new CacheControlHeaderValue()
            {
                MaxAge = Expiry,
                MustRevalidate = true,
                Public = IsPublic,
                Private = !IsPublic
            };
        }
        protected override bool ShouldTryExtract()
        {
            return true;
        }
    }
}
