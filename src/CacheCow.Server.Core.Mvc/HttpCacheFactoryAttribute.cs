using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server.Core.Mvc
{
    /// <summary>
    /// 
    /// </summary>
    public class HttpCacheFactoryAttribute : Attribute, IFilterFactory
    {
        public bool IsReusable => false;

        /// <summary>
        /// Set this property if the resource does a constant expiry
        /// </summary>
        public int? ExpirySeconds { get; set; }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var cacheFilter = serviceProvider.GetService<HttpCacheFilter>();
            cacheFilter.ExpirySeconds = ExpirySeconds;
            return cacheFilter;
        }
    }
}
