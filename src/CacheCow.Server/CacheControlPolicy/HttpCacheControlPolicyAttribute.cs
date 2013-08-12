using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Server.CacheControlPolicy
{

    [Obsolete("This class is moving to its own dll after version 0.5: CacheCow.Server.WebHost.CachePolicies")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HttpCacheControlPolicyAttribute : Attribute
    {

        private readonly CacheControlHeaderValue _cacheControl;

        /// <summary>
        /// default .ctor is no cache policy
        /// </summary>
        public HttpCacheControlPolicyAttribute()
        {

            _cacheControl = new CacheControlHeaderValue()
                                {
                                    Private = true,
                                    NoCache = true,
                                    NoStore = true
                                };
        }

        public HttpCacheControlPolicyAttribute(bool isPrivate, 
            int maxAgeInSeconds, 
            bool mustRevalidate = true,
            bool noCache = false,
            bool noTransform = false) : this()
        {
            _cacheControl = new CacheControlHeaderValue()
            {
                Private = isPrivate,
                Public = !isPrivate,
                MustRevalidate = mustRevalidate,
                MaxAge = TimeSpan.FromSeconds(maxAgeInSeconds),
                NoCache = noCache,
                NoTransform = noTransform   
            };
        }

        public CacheControlHeaderValue CacheControl
        {
            get { return _cacheControl; }
        }
    }
}
