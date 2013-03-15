using System;
using System.Net.Http;
using System.Web.Http;

namespace CacheCow.Server.CacheRefreshPolicy
{
    public abstract class CacheRefreshPolicyBase : ICacheRefreshPolicy
    {
        private TimeSpan _defaultRefreshInterval;

        public CacheRefreshPolicyBase() : this(TimeSpan.MaxValue)
        {
            
        }

        public CacheRefreshPolicyBase(TimeSpan defaultRefreshInterval)
        {
            _defaultRefreshInterval = defaultRefreshInterval;
        }

        public TimeSpan GetCacheRefreshPolicy(HttpRequestMessage request, HttpConfiguration configuration)
        {
            return DoGetCacheRefreshPolicy(request, configuration) ?? _defaultRefreshInterval;
        }

        public abstract TimeSpan? DoGetCacheRefreshPolicy(HttpRequestMessage request, HttpConfiguration configuration);
    }
}
