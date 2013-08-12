using System;


namespace CacheCow.Server.CacheRefreshPolicy
{
    [Obsolete("This class is moving to its own dll after version 0.5: CacheCow.Server.WebHost.CachePolicies")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HttpCacheRefreshPolicyAttribute : Attribute
    {
        private readonly TimeSpan _refreshInterval;

        public HttpCacheRefreshPolicyAttribute(int refreshIntervalInSeconds)
        {
            _refreshInterval = TimeSpan.FromSeconds(refreshIntervalInSeconds);
        }

        public TimeSpan RefreshInterval
        {
            get { return _refreshInterval; }
        }
    }
}
