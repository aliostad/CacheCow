using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
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

            if(filter == null)
            {
                throw new InvalidOperationException("Could not resolve the filter or its dependencies. If you have defined ViewModelType, make sure at least one generic registerations are done using ConfigurationExtensions.");
            }

            filter.ConfiguredExpiry = _expirySeconds.HasValue
                ? (TimeSpan?) TimeSpan.FromSeconds(_expirySeconds.Value)
                : null;


            return filter;
        }
    }
}
