using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Hosting;
using CacheCow.Server.CacheControlPolicy;
using NUnit.Framework;

namespace CacheCow.Tests.Server.CacheControlPolicy
{
    [TestFixture]
    public class AttributeBasedCachePolicyTests
    {

        [Test]
        public void TestControllerLevel()
        {
            var configuration = new HttpConfiguration(new HttpRouteCollection("/"));
            configuration.Routes.MapHttpRoute("main", "api/{controller}/{id}");
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://aliostad/api/CachePolicy/1"));
            var routeData = configuration.Routes.GetRouteData(request);
            request.Properties.Add(HttpPropertyKeys.HttpRouteDataKey, (object)routeData);
            

            var attributeBasedCachePolicy = new AttributeBasedCacheControlPolicy(new CacheControlHeaderValue());
            var cchv = attributeBasedCachePolicy.GetCacheControl(request, configuration);

            Assert.AreEqual(TimeSpan.FromSeconds(110), cchv.MaxAge);
            Assert.AreEqual(false, cchv.Private, "Private");

        }

        [Test]
        public void TestControllerAndActionLevelPolicy()
        {
            var configuration = new HttpConfiguration(new HttpRouteCollection("/"));
            configuration.Routes.MapHttpRoute("main", "api/{controller}/{id}");
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://aliostad/api/CachePolicyAction/1"));
            var routeData = configuration.Routes.GetRouteData(request);
            request.Properties.Add(HttpPropertyKeys.HttpRouteDataKey, (object)routeData);
            var attributeBasedCachePolicy = new AttributeBasedCacheControlPolicy(new CacheControlHeaderValue());

            // act
            var cchv = attributeBasedCachePolicy.GetCacheControl(request, configuration);

            // assert
            Assert.AreEqual(TimeSpan.FromSeconds(120), cchv.MaxAge);
            Assert.AreEqual(false, cchv.Private, "Private");

        }

        [Test]
        public void TestDefaultControllerOrActionLevelPolicy()
        {
            // arrange
            var configuration = new HttpConfiguration(new HttpRouteCollection("/"));
            configuration.Routes.MapHttpRoute("main", "api/{controller}/{id}");
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://aliostad/api/NoCachePolicy/1"));
            var routeData = configuration.Routes.GetRouteData(request);
            request.Properties.Add(HttpPropertyKeys.HttpRouteDataKey, (object)routeData);
            var attributeBasedCachePolicy = new AttributeBasedCacheControlPolicy(new CacheControlHeaderValue()
                {NoStore = true, NoCache = true});

            // action
            var cchv = attributeBasedCachePolicy.GetCacheControl(request, configuration);

            // assert
            Assert.AreEqual(true, cchv.NoCache, "NoCache");
            Assert.AreEqual(true, cchv.NoStore, "NoStore");

        }
    }


   
}

namespace CacheCow.Tests.Server.CachePolicy.Controllers
{
    [HttpCachePolicy(false, 110)]
    public class CachePolicyController : ApiController
    {
        public string Get(int id)
        {
            return "CacheCow";
        }
    }

    [HttpCachePolicy(false, 110)]
    public class CachePolicyActionController : ApiController
    {
        [HttpCachePolicy(false, 120)]
        public string Get(int id)
        {
            return "CacheCow";
        }
    }

    public class NoCachePolicyController : ApiController
    {
        public string Get(int id)
        {
            return "CacheCow";
        }
    }    
}
