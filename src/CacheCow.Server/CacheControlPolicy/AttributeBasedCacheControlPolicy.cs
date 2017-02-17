using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace CacheCow.Server.CacheControlPolicy
{

    public class AttributeBasedCacheControlPolicy : CacheControlPolicyBase
    {
        public AttributeBasedCacheControlPolicy(CacheControlHeaderValue defaultValue) : base(defaultValue)
        {
        }

        protected override CacheControlHeaderValue DoGetCacheControl(HttpRequestMessage request, HttpConfiguration configuration)
        {
            var httpRouteData = request.GetRouteData();
            if (httpRouteData == null)
                return null;

            // call these first
            var controllerSelector = configuration.Services.GetHttpControllerSelector();

            try
            {
                var controllerDescriptor = controllerSelector.SelectController(request);

                // first check the action
                var controllerContext = new HttpControllerContext(configuration, httpRouteData, request)
                {
                    ControllerDescriptor = controllerDescriptor
                };
                var httpActionSelector = configuration.Services.GetActionSelector();


                var actionDescriptor = httpActionSelector.SelectAction(controllerContext);
                var cachePolicyAttribute = actionDescriptor.GetCustomAttributes<HttpCacheControlPolicyAttribute>().FirstOrDefault();
                if (cachePolicyAttribute != null)
                    return cachePolicyAttribute.CacheControl;

                // now check controller
                var controllerPolicy = controllerDescriptor.GetCustomAttributes<HttpCacheControlPolicyAttribute>().FirstOrDefault();

                return controllerPolicy == null ? null : controllerPolicy.CacheControl;

            }
            catch (HttpResponseException)
            {

                return null;
            }

        }

        private Type GetControllerByName(string name)
        {
            name += "Controller";
            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes());
            return allTypes.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                                                && x.IsAssignableFrom(typeof(ApiController)));
        }
    }
}
