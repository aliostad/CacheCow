using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Server.CachePolicy
{

    public class CachePolicyHeader : CacheControlHeaderValue
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HttpCachePolicyAttribute : Attribute
    {

        private readonly CacheControlHeaderValue _cacheControl;

        /// <summary>
        /// default .ctor is no cache policy
        /// </summary>
        public HttpCachePolicyAttribute()
        {

            _cacheControl = new CacheControlHeaderValue()
                                {
                                    Private = true,
                                    NoCache = true,
                                    NoStore = true
                                };
        }

        public HttpCachePolicyAttribute(bool isPrivate, int maxAgeInSeconds) : this()
        {
            _cacheControl = new CacheControlHeaderValue()
            {
                Private = isPrivate,
                Public = !isPrivate,
                MustRevalidate = true,
                MaxAge = TimeSpan.FromSeconds(maxAgeInSeconds)
            };
        }

        public CacheControlHeaderValue CacheControl
        {
            get { return _cacheControl; }
        }
    }
}
