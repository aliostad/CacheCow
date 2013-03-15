using System;
using System.Net.Http;
using System.Web.Http;

namespace CacheCow.Server.CacheRefreshPolicy
{
    public abstract class CacheRefreshPolicyBase : ICacheRefreshPolicy
    {
        private TimeSpan _defaultExpiry;

        public CacheRefreshPolicyBase() : this(TimeSpan.MaxValue)
        {
            
        }

        public CacheRefreshPolicyBase(TimeSpan defaultExpiry)
        {
            _defaultExpiry = defaultExpiry;
        }

        public TimeSpan GetCacheControl(HttpRequestMessage request, HttpConfiguration configuration)
        {
            return DoGetCacheControl(request, configuration) ?? _defaultExpiry;
        }

        public abstract TimeSpan? DoGetCacheControl(HttpRequestMessage request, HttpConfiguration configuration);
    }
}
