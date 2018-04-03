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
        /// Set this property if the resource does a constant expiry.
        /// 
        /// </summary>
        public int? ExpirySeconds { get; set; }

        /// <summary>
        /// Type parameter for ITimedETagQueryProvider&lt;T&gt; and ICacheDirectiveProvider&lt;T&gt;. 
        /// A decorative parameter for the ease of service location.
        /// </summary>
        public Type ViewModelType { get; set; }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            HttpCacheFilter filter = null;
            if(ViewModelType == null)
            {
                filter = serviceProvider.GetService<HttpCacheFilter>();
            }
            else
            {
                var t = typeof(HttpCacheFilter<>);
                var filterType = t.MakeGenericType(ViewModelType);
                filter = (HttpCacheFilter)serviceProvider.GetService(filterType);
            }
            
            if(ExpirySeconds.HasValue)
            {
                filter.CacheDirectiveProvider = ExpirySeconds.Value == 0 ?
                    (ICacheDirectiveProvider) serviceProvider.GetService<StrongConsistencyProvider>() :
                    serviceProvider.GetService<ConstantExpiryProvider>();
            }

            return filter;
        }
    }
}
