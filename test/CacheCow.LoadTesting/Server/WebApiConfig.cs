using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using CacheCow.Server;
using CacheCow.Server.CacheControlPolicy;
using CacheCow.Server.CacheRefreshPolicy;

namespace CacheCow.LoadTesting.Server
{
    class WebApiConfig
    {
        static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "PricingApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new
                {
                    controller = "Test",
                    id = RouteParameter.Optional
                });

            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            config.Formatters.XmlFormatter.UseXmlSerializer = true;
            var cachingHandler = new CachingHandler("Accept", "Accept-Encoding");
            cachingHandler.CacheControlHeaderProvider = new AttributeBasedCacheControlPolicy(new CacheControlHeaderValue()
                {
                    NoCache = true, Private = true, NoStore = true
                }).GetCacheControl; // turn caching off unless an attribute is used

            cachingHandler.CacheRefreshPolicyProvider = new AttributeBasedCacheRefreshPolicy(TimeSpan.FromMinutes(15)).GetCacheRefreshPolicy;
            config.MessageHandlers.Add(cachingHandler);
        }
    }
}
