using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace CacheCow.Server.CacheControlPolicy
{
    public class AttributeBasedRoutingCacheControlPolicy : CacheControlPolicyBase
    {
        public AttributeBasedRoutingCacheControlPolicy(CacheControlHeaderValue defaultValue) : base(defaultValue)
        {
        }

        protected override CacheControlHeaderValue DoGetCacheControl(HttpRequestMessage request, HttpConfiguration configuration)
        {
            var httpRouteData = request.GetRouteData();
            if (httpRouteData == null)
            {
                return null;
            }

            try
            {
                var controllerDescriptor = configuration.Services.GetHttpControllerSelector().SelectController(request);

                object actions;
                if (httpRouteData.Route.DataTokens.TryGetValue("actions", out actions))
                {
                    var action = ((HttpActionDescriptor[])actions).Single();

                    var cachePolicyAttribute = action.GetCustomAttributes<HttpCacheControlPolicyAttribute>().FirstOrDefault();
                    if (cachePolicyAttribute != null)
                    {
                        return cachePolicyAttribute.CacheControl;
                    }
                }

                // now check controller
                var controllerPolicy = controllerDescriptor.GetCustomAttributes<HttpCacheControlPolicyAttribute>().FirstOrDefault();

                return controllerPolicy == null ? null : controllerPolicy.CacheControl;
            }
            catch (HttpResponseException)
            {
                return null;
            }
        }
    }
}
