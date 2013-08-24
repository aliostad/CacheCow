using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.Hosting;
using CacheCow.Server.CacheControlPolicy;
using CacheCow.Tests.Common;
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
        public void TestRefreshPolicyFor404()
        {
            var configuration = new HttpConfiguration(new HttpRouteCollection("/"));
            configuration.Services.Replace(typeof (IHttpControllerSelector), new NotFoundControllerSelector());
            configuration.Routes.MapHttpRoute("main", "api/{controller}/{id}");
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://aliostad/api/CachePolicyAction/1"));
            var routeData = configuration.Routes.GetRouteData(request);
            request.Properties.Add(HttpPropertyKeys.HttpRouteDataKey, (object)routeData);
            var headerValue = new CacheControlHeaderValue();
            var attributeBasedCachePolicy = new AttributeBasedCacheControlPolicy(headerValue);

            // act
            var cchv = attributeBasedCachePolicy.GetCacheControl(request, configuration);

            // assert
            Assert.AreEqual(headerValue, cchv);

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

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestHttpCacheControlPolicyAttributeFactoryConstructor_WithInvalidType()
        {
            var httpCacheControlPolicyAttribute = new HttpCacheControlPolicyAttribute(typeof(object));
        }

        [Test]
        public void TestHttpCacheControlPolicyAttributeFactoryConstructor_WithValidType()
        {
            // arrange, act
            var httpCacheControlPolicyAttribute = new HttpCacheControlPolicyAttribute(typeof(CacheControlFactory));

            // assert
            Assert.AreEqual(CacheControlFactory.Header, httpCacheControlPolicyAttribute.CacheControl);

        }

        public class CacheControlFactory
        {
            public static CacheControlHeaderValue Header = new CacheControlHeaderValue();

            public CacheControlHeaderValue GetHeader()
            {
                return Header;
            }
        }
    }


   
}

namespace CacheCow.Tests.Server.CachePolicy.Controllers
{
    [HttpCacheControlPolicy(false, 110)]
    public class CachePolicyController : ApiController
    {
        public string Get(int id)
        {
            return "CacheCow";
        }
    }

    [HttpCacheControlPolicy(false, 110)]
    public class CachePolicyActionController : ApiController
    {
        [HttpCacheControlPolicy(false, 120)]
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
