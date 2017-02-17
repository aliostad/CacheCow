using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.Hosting;
using CacheCow.Server.CacheControlPolicy;
using CacheCow.Server.CacheRefreshPolicy;
using CacheCow.Tests.Common;
using NUnit.Framework;

namespace CacheCow.Tests.Server.CacheRefreshPolicy
{
    [TestFixture]
    public class AttributeBasedCacheRefreshPolicyTests
    {

        [Test]
        public void TestControllerLevelPolicy()
        {
            var configuration = new HttpConfiguration(new HttpRouteCollection("/"));
            configuration.Routes.MapHttpRoute("main", "api/{controller}/{id}");
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://aliostad/api/CacheRefreshPolicy/1"));
            var routeData = configuration.Routes.GetRouteData(request);
            request.Properties.Add(HttpPropertyKeys.HttpRouteDataKey, (object)routeData);
            var attributeBasedCachePolicy = new AttributeBasedCacheRefreshPolicy();


            // act
            var refresh = attributeBasedCachePolicy.DoGetCacheRefreshPolicy(request, configuration);

            // assert
            Assert.AreEqual(true, refresh.HasValue);
            Assert.AreEqual(TimeSpan.FromSeconds(110), refresh.Value);

        }

        [Test]
        public void TestControllerAndActionLevelPolicy()
        {
            var configuration = new HttpConfiguration(new HttpRouteCollection("/"));
            configuration.Routes.MapHttpRoute("main", "api/{controller}/{id}");
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://aliostad/api/CacheRefreshPolicyAction/1"));
            var routeData = configuration.Routes.GetRouteData(request);
            request.Properties.Add(HttpPropertyKeys.HttpRouteDataKey, (object)routeData);
            var attributeBasedCachePolicy = new AttributeBasedCacheRefreshPolicy();

            // act
            var refresh = attributeBasedCachePolicy.DoGetCacheRefreshPolicy(request, configuration);

            // assert
            Assert.AreEqual(true, refresh.HasValue);
            Assert.AreEqual(TimeSpan.FromSeconds(120), refresh.Value);


        }



        [Test]
        public void TestRefreshPolicyFor404()
        {
            var configuration = new HttpConfiguration(new HttpRouteCollection("/"));
            configuration.Routes.MapHttpRoute("main", "api/{controller}/{id}");
            configuration.Services.Replace(typeof(IHttpControllerSelector), new NotFoundControllerSelector());
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://aliostad/api/CacheRefreshPolicyAction/1"));
            var routeData = configuration.Routes.GetRouteData(request);
            request.Properties.Add(HttpPropertyKeys.HttpRouteDataKey, (object)routeData);
            var attributeBasedCachePolicy = new AttributeBasedCacheRefreshPolicy();

            // act
            var refresh = attributeBasedCachePolicy.DoGetCacheRefreshPolicy(request, configuration);

            // assert
            Assert.AreEqual(false, refresh.HasValue);


        }
        [Test]
        public void TestDefaultControllerOrActionLevelPolicy()
        {
            // arrange
            var configuration = new HttpConfiguration(new HttpRouteCollection("/"));
            configuration.Routes.MapHttpRoute("main", "api/{controller}/{id}");
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://aliostad/api/NoCacheRefreshPolicy/1"));
            var routeData = configuration.Routes.GetRouteData(request);
            request.Properties.Add(HttpPropertyKeys.HttpRouteDataKey, (object)routeData);
            var attributeBasedCachePolicy = new AttributeBasedCacheRefreshPolicy();

            // act
            var refresh = attributeBasedCachePolicy.DoGetCacheRefreshPolicy(request, configuration);

            // assert
            Assert.AreEqual(false, refresh.HasValue);


        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestHttpCacheRefreshPolicyAttributeFactoryConstructor_WithInvalidType()
        {
            var httpCacheControlPolicyAttribute = new HttpCacheRefreshPolicyAttribute(typeof(object));
        }

        [Test]
        public void TestHttpCacheControlRefreshAttributeFactoryConstructor_WithValidType()
        {
            // arrange, act
            var httpCacheControlPolicyAttribute = new HttpCacheRefreshPolicyAttribute(typeof(CacheRefreshFactory));

            // assert
            Assert.AreEqual(CacheRefreshFactory.Interval, httpCacheControlPolicyAttribute.RefreshInterval);

        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void TestHttpCacheRefreshPolicyAttributeAppSettingConstructor_WithInvalidValue()
        {
            var httpCacheControlPolicyAttribute = new HttpCacheRefreshPolicyAttribute("RefreshSeconds-Invalid");
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestHttpCacheRefreshPolicyAttributeAppSettingConstructor_WithNonExistentValue()
        {
            var httpCacheControlPolicyAttribute = new HttpCacheRefreshPolicyAttribute("RefreshSeconds-NonExistent");
        }

        [Test]
        public void TestHttpCacheRefreshPolicyAttributeAppSettingConstructor_WithValidValue()
        {
            var httpCacheControlPolicyAttribute = new HttpCacheRefreshPolicyAttribute("RefreshSeconds-Valid");
            Assert.AreEqual(TimeSpan.FromSeconds(112), httpCacheControlPolicyAttribute.RefreshInterval);
        }


        public class CacheRefreshFactory
        {
            public static TimeSpan Interval = TimeSpan.FromSeconds(15 * 60);

            public TimeSpan GetHeader()
            {
                return Interval;
            }
        }
    }



}

namespace CacheCow.Tests.Server.CacheRefreshPolicy.Controllers
{
    [HttpCacheRefreshPolicy(110)]
    public class CacheRefreshPolicyController : ApiController
    {
        public string Get(int id)
        {
            return "CacheCow";
        }
    }

    [HttpCacheRefreshPolicy(110)]
    public class CacheRefreshPolicyActionController : ApiController
    {
        [HttpCacheRefreshPolicy(120)]
        public string Get(int id)
        {
            return "CacheCow";
        }
    }

    public class NoCacheRefreshPolicyController : ApiController
    {
        public string Get(int id)
        {
            return "CacheCow";
        }
    }
}
