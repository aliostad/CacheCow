using System;
using System.Diagnostics;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Hosting;
using CacheCow.Server.RoutePatternPolicy;
using NUnit.Framework;
using System.Linq;

namespace CacheCow.Tests.Server.RoutePatternPolicy
{
    [TestFixture]
    public class ConventionalRoutePatternProviderTests
    {

        [Test]
        public void GetRoutePattern_NoRouteData()
        {
            var configuration = GetDefaultConfig();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://google.com/x/y/z?a=b&c=d");
            var routePatternProvider = new ConventionalRoutePatternProvider(configuration);

            Assert.AreEqual("/x/y/z", routePatternProvider.GetRoutePattern(request));

        }

        [TestCase("api/{controller}/{id}", "http://x/api/y/123", "/api/y/+")]
        [TestCase("api/{controller}/{id}", "http://x/api/y/", "/api/y/*")]
        [TestCase("api/{controller}/{id}", "http://x/api/y", "/api/y/*")]
        [TestCase("api/{controller}/{chichak}/{id}", "http://x/api/y", "/api/y//*")]
        [TestCase("api/{topcontroller}/{topid}/{controller}/{id}", "http://x/api/y/1/x", "/api/y/1/x/*")]
        [TestCase("api/{topcontroller}/{topid}/{controller}/{id}", "http://x/api/y/1/x/aliostad", "/api/y/1/x/+")]
        [TestCase("api/{topcontroller}/{topid}/{controller}/{action}", "http://x/api/y/1/x/aliostad", "/api/y/1/x/*")]
        public void BuildRoutePattern(string routeTemplate, string url, string exptectedPattern)
        {
            // arg
            var configuration = new HttpConfiguration();
            configuration.Routes.MapHttpRoute("test", routeTemplate, new
            {
                id = RouteParameter.Optional,
                chichak = RouteParameter.Optional
            });
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var routePatternProvider = new ConventionalRoutePatternProvider(configuration);
            var routeData = configuration.Routes.GetRouteData(request);
            request.Properties.Add(HttpPropertyKeys.HttpRouteDataKey, routeData);

            // act
            var routePattern = routePatternProvider.GetRoutePattern(request);

            // asrt
            Assert.AreEqual(exptectedPattern, routePattern);
        }

        [TestCase("api/{controller}/{id}", "http://x/api/y/123", new[] { "/api/y/*" })]
        [TestCase("api/{controller}/{id}", "http://x/api/y/", new[] { "/api" })]
        [TestCase("api/{controller}/{id}", "http://x/api/y", new[] { "/api" })]
        [TestCase("api/{controller}/{chichak}/{id}", "http://x/api/y", new[] { "/api/y" })]
        [TestCase("api/{parentController}/{parentId}/{controller}/{id}", "http://x/api/y/123/z/12", new[] { "/api/y/123" })]
        [TestCase("api/{parentController}/{parentId}/{controller}/{id}", "http://x/api/y/123/z/", new[] { "/api/y/123" })]
        public void GetLinkedRoutePatterns(string routeTemplate, string url, string[] exptectedPatterns)
        {
            var configuration = new HttpConfiguration();
            configuration.Routes.MapHttpRoute("test", routeTemplate, new
            {
                id = RouteParameter.Optional,
                chichak = RouteParameter.Optional
            });
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var routePatternProvider = new ConventionalRoutePatternProvider(configuration);
            var routeData = configuration.Routes.GetRouteData(request);
            request.Properties.Add(HttpPropertyKeys.HttpRouteDataKey, routeData);

            var routePatterns = routePatternProvider.GetLinkedRoutePatterns(request);

            foreach (var exptectedPattern in exptectedPatterns)
            {
                Console.WriteLine(exptectedPattern);

                if (routePatterns.All(x => x != exptectedPattern))
                {
                    Console.WriteLine("These were returned:");
                    routePatterns.ToList().ForEach(x => Console.WriteLine("\t" + x));
                    Assert.Fail("could not find " + exptectedPattern);
                }
            }

        }

        private static HttpConfiguration GetDefaultConfig()
        {
            var configuration = new HttpConfiguration();
            configuration.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{chichak}/{controller}/{id}",
                defaults: new
                {
                    id = RouteParameter.Optional,
                    chichak = RouteParameter.Optional
                }
            );
            return configuration;
        }
    }
}
