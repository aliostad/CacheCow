using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using CacheCow.Server;
using Microsoft.Owin;
using Nancy.Owin;
using Owin;

namespace UsingCacheCowWithNancyAndOwin
{

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var cachingHandler = new CachingHandler(new HttpConfiguration(), new InMemoryEntityTagStore(), "Accept");
            cachingHandler.CacheControlHeaderProvider =
                (message, configuration) => new CacheControlHeaderValue()
                                                {
                                                    //NoCache = true,
                                                    //MaxAge = TimeSpan.FromSeconds(100)
                                                };

            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            
            app.UseWebApi(config, (q,s) => true);
            //app.UseHttpMessageHandler(new MyClass());                                                       
            //app.UseHttpMessageHandler(new MyClass2());
            //app.UseNancy();            
            app.UseHttpMessageHandler(new OwinHandlerBridge(cachingHandler), (q,s) => false);
        }

        private class MyClass : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
                CancellationToken cancellationToken)
            {
                return Task.FromResult(request.CreateResponse(HttpStatusCode.Conflict));
            }
        }

        private class MyClass2 : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(request.CreateResponse(HttpStatusCode.BadGateway));
            }
        }
    }

}