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

        public TimeSpan GetCacheControl(HttpRequestMessage request, HttpConfiguration configuration)
        {
            return DoGetCacheControl(request, configuration) ?? _defaultRefreshInterval;
        }

        public abstract TimeSpan? DoGetCacheControl(HttpRequestMessage request, HttpConfiguration configuration);
    }
}
