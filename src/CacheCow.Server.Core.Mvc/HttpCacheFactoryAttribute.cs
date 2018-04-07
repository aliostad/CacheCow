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
        private int? _expirySeconds;

        public HttpCacheFactoryAttribute()
        {

        }

        public HttpCacheFactoryAttribute(int expirySeconds)
        {
            _expirySeconds = expirySeconds;
        }

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

            filter.ConfiguredExpiry = _expirySeconds.HasValue
                ? (TimeSpan?) TimeSpan.FromSeconds(_expirySeconds.Value)
                : null;


            return filter;
        }
    }
}
