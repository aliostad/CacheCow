using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace CacheCow.Server.Core
{
    /// <summary>
    /// Default cache directive provider
    /// </summary>
    public class NoCacheNoStoreProvider : CacheDirectiveProviderBase
    {
        public NoCacheNoStoreProvider(ITimedETagExtractor timedETagExtractor) : base(timedETagExtractor)
        {
        }

        public override CacheControlHeaderValue Get(HttpContext context)
        {
            return new CacheControlHeaderValue()
            {
                NoCache = true,
                NoStore = true
            };
        }

        protected override bool ShouldTryExtract()
        {
            return false;
        }
    }

    public class NoCacheNoStoreProvider<TViewModel> : NoCacheNoStoreProvider, ICacheDirectiveProvider<TViewModel>
    {
        public NoCacheNoStoreProvider(ITimedETagExtractor timedETagExtractor) : base(timedETagExtractor)
        {
        }
    }
}
