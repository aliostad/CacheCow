using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CacheCow.Server.Core.Mvc
{
    /// <summary>
    /// A resource filter responsibility for implementing HTTP Caching
    /// Use it with HttpCacheFactoryAttribute
    /// </summary>
    public class HttpCacheFilter : IAsyncResourceFilter
    { 
        private ICacheabilityValidator _validator;

        public HttpCacheFilter(ICacheabilityValidator validator)
        {
            _validator = validator;
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            bool isCacheable = _validator.IsCacheable(context.HttpContext.Request);
            var execCtx = await next();
            if (!isCacheable || execCtx.Canceled || execCtx.Exception != null || _validator.IsCacheable(context.HttpContext.Response))
                return;


           // if(execCtx.)
           
        }

        
      

        /// <summary>
        /// Set this property if the resource does a constant expiry
        /// </summary>
        public int? ExpirySeconds { get; set; }
        
    }
}
