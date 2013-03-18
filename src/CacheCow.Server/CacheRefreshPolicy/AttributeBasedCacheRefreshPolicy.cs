using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Controllers;
using CacheCow.Server.CacheControlPolicy;

namespace CacheCow.Server.CacheRefreshPolicy
{
    public class AttributeBasedCacheRefreshPolicy : CacheRefreshPolicyBase
    {
        public AttributeBasedCacheRefreshPolicy() : base()
        {
            
        }

        public AttributeBasedCacheRefreshPolicy(TimeSpan defaultRefreshInterval)
            :base(defaultRefreshInterval)
        {

        }

        public override TimeSpan? DoGetCacheRefreshPolicy(HttpRequestMessage request, HttpConfiguration configuration)
        {
            var httpRouteData = request.GetRouteData();
            if (httpRouteData == null)
                return null;

            // call these first
            var controllerSelector = configuration.Services.GetHttpControllerSelector();
            var controllerDescriptor = controllerSelector.SelectController(request);

            // first check the action
            var controllerContext = new HttpControllerContext(configuration, httpRouteData, request)
            {
                ControllerDescriptor = controllerDescriptor
            };
            var httpActionSelector = configuration.Services.GetActionSelector();


            var actionDescriptor = httpActionSelector.SelectAction(controllerContext);
            var cachePolicyAttribute = actionDescriptor.GetCustomAttributes<HttpCacheRefreshPolicyAttribute>().FirstOrDefault();
            if (cachePolicyAttribute != null)
                return cachePolicyAttribute.RefreshInterval;

            // now check controller
            var controllerPolicy = controllerDescriptor.GetCustomAttributes<HttpCacheRefreshPolicyAttribute>().FirstOrDefault();

            return controllerPolicy == null ? (TimeSpan?) null : controllerPolicy.RefreshInterval;
        }
    }
}
