using System;


namespace CacheCow.Server.CacheRefreshPolicy
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HttpCacheRefreshAttribute : Attribute
    {
        private readonly TimeSpan _refreshInterval;

        public HttpCacheRefreshAttribute(int refreshIntervalInSeconds)
        {
            _refreshInterval = TimeSpan.FromSeconds(refreshIntervalInSeconds);
        }

        public TimeSpan RefreshInterval
        {
            get { return _refreshInterval; }
        }
    }
}
