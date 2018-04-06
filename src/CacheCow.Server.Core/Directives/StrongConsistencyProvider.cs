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
    public class StrongConsistencyProvider : CacheDirectiveProviderBase
    {
        public StrongConsistencyProvider(ITimedETagExtractor timedETagExtractor) : base(timedETagExtractor)
        {
        }

        public override CacheControlHeaderValue Get(HttpContext context)
        {
            return new CacheControlHeaderValue()
            {
                Private = true, // because cache intermediaries can be sloppy
                MustRevalidate = true,
                MaxAge = TimeSpan.Zero
            };
        }

        protected override bool ShouldTryExtract()
        {
            return true;
        }
    }
}
