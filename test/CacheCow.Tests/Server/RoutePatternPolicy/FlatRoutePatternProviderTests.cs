using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Hosting;
using CacheCow.Server.RoutePatternPolicy;
using NUnit.Framework;

namespace CacheCow.Tests.Server.RoutePatternPolicy
{
    [TestFixture]
    public class FlatRoutePatternProviderTests
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
        [TestCase("api/{topcontroller}/{topid}/{controller}/{id}", "http://x/api/y/1/x", "/api/y/1/x/*")]
        [TestCase("api/{topcontroller}/{topid}/{controller}/{id}", "http://x/api/y/1/x/aliostad", "/api/y/1/x/+")]
        [TestCase("api/{topcontroller}/{topid}/{controller}/{action}", "http://x/api/y/1/x/aliostad", "/api/y/1/x/*")]
        public void BuildRoutePattern(string routeTemplate, string url, string exptectedPattern)
        {
            var configuration = new HttpConfiguration();
            configuration.Routes.MapHttpRoute("test", routeTemplate, new {id = RouteParameter.Optional});
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var routePatternProvider = new ConventionalRoutePatternProvider(configuration);
            var routeData = configuration.Routes.GetRouteData(request);
            request.Properties.Add(HttpPropertyKeys.HttpRouteDataKey, routeData);

            var routePattern = routePatternProvider.GetRoutePattern(request);

            Assert.AreEqual(exptectedPattern, routePattern);
        }


        private static HttpConfiguration GetDefaultConfig()
        {
            var configuration = new HttpConfiguration();
            configuration.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            return configuration;
        }
    }
}
