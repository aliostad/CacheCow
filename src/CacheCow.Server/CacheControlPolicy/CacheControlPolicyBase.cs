using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using CacheCow.Server.CacheControlPolicy;

namespace CacheCow.Server.CacheControlPolicy
{
    public abstract class CacheControlPolicyBase : ICacheControlPolicy
    {
        private CacheControlHeaderValue _defaultValue;

        public CacheControlPolicyBase(CacheControlHeaderValue defaultValue)
        {
            _defaultValue = defaultValue;
        }

        public CacheControlHeaderValue GetCacheControl(HttpRequestMessage request, HttpConfiguration configuration)
        {
            var cacheControlHeaderValue = DoGetCacheControl(request, configuration);
            return cacheControlHeaderValue ?? CloneCacheControlHeaderValue(_defaultValue);
        }

        protected abstract CacheControlHeaderValue DoGetCacheControl(HttpRequestMessage request, HttpConfiguration configuration);

        protected CacheControlHeaderValue CloneCacheControlHeaderValue(CacheControlHeaderValue headerValue)
        {
            return new CacheControlHeaderValue()
                       {
                           MaxAge = headerValue.MaxAge,
                           MaxStale = headerValue.MaxStale,
                           MaxStaleLimit = headerValue.MaxStaleLimit,
                           MinFresh = headerValue.MinFresh,
                           MustRevalidate = headerValue.MustRevalidate,
                           NoCache = headerValue.NoCache,
                           NoStore = headerValue.NoStore,
                           NoTransform = headerValue.NoTransform,
                           OnlyIfCached = headerValue.OnlyIfCached,
                           Private = headerValue.Private,
                           ProxyRevalidate = headerValue.ProxyRevalidate,
                           Public = headerValue.Public,
                           SharedMaxAge = headerValue.SharedMaxAge
                       };
        }
    }
}
