using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace CacheCow.Server.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class ConstantExpiryProvider : ICacheDirectiveProvider
    {
        private readonly bool _isPublic;
        private readonly bool _mustRevalidate;

        /// <summary>
        /// 
        /// </summary>
        public ConstantExpiryProvider(TimeSpan expiry, bool isPublic = true, bool mustRevalidate = true)
        {
            Expiry = expiry;
            this._isPublic = isPublic;
            this._mustRevalidate = mustRevalidate;
        }

        public TimeSpan Expiry { get; }

        public void Dispose()
        {
            // nilch
        }

        public CacheControlHeaderValue Get(HttpContext context)
        {
            return new CacheControlHeaderValue()
            {
                MaxAge = Expiry,
                MustRevalidate = _mustRevalidate,
                Private = !_isPublic,
                Public = _isPublic
            };
        }
    }
}
