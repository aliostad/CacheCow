using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace CacheCow.Server.Core
{
    public class DefaultCacheDirectiveProvider : CacheDirectiveProviderBase
    {
        public DefaultCacheDirectiveProvider(ITimedETagExtractor timedETagExtractor, 
            ITimedETagQueryProvider queryProvider) : base(timedETagExtractor, queryProvider)
        {
        }

        public override CacheControlHeaderValue GetCacheControl(HttpContext context, TimeSpan? configuredExpiry)
        {
            switch(configuredExpiry)
            {
                case TimeSpan t when t == TimeSpan.Zero:
                    return new CacheControlHeaderValue() { MaxAge = TimeSpan.Zero, Private = true, MustRevalidate = true };
                case TimeSpan t:
                    return new CacheControlHeaderValue() { MaxAge = t, Public = true, MustRevalidate = true };
                default:
                    return new CacheControlHeaderValue() { NoCache = true, NoStore = true };
            }
        }
    }

    public class DefaultCacheDirectiveProvider<TViewModel> : DefaultCacheDirectiveProvider, ICacheDirectiveProvider<TViewModel>
    {
        public DefaultCacheDirectiveProvider(ITimedETagExtractor<TViewModel> timedETagExtractor, 
            ITimedETagQueryProvider<TViewModel> queryProvider) : base(timedETagExtractor, queryProvider)
        {
        }
    }
}
