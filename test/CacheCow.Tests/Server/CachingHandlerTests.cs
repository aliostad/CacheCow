using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using CacheCow.Common;
using CacheCow.Common.Helpers;
using CacheCow.Server;
using Moq;
using NUnit.Framework;

namespace CacheCow.Tests.Server
{
    using System.IO;

    public class CachingHandlerTests
    {
        private const string TestUrl = "http://myserver/api/stuff/";
        private const string TestUrl2 = "http://myserver/api/more/";

        private static readonly string[] EtagValues = new[] { "abcdefgh", "12345678" };

        [TestCase("DELETE")]
        [TestCase("PUT")]
        [TestCase("POST")]
        [TestCase("PATCH")]
        public static void TestCacheInvalidation(string method)
        {
            // setup
            var request = new HttpRequestMessage(new HttpMethod(method), TestUrl);
            string routePattern = "http://myserver/api/stuffs/*";
            var entityTagStore = new Mock<IEntityTagStore>();
            var cachingHandler = new CachingHandler(new HttpConfiguration(), entityTagStore.Object)
                                    {

                                    };
            var entityTagKey = new CacheKey(TestUrl, new string[0], routePattern);
            var response = new HttpResponseMessage();
            var invalidateCache = cachingHandler.InvalidateCache(entityTagKey, request, response);
            entityTagStore.Setup(x => x.RemoveResourceAsync("/api/stuff/")).Returns(Task.FromResult(1));

            // run
            invalidateCache();

            // verify

        }




        [TestCase("PUT")]
        [TestCase("POST")]
        [TestCase("PATCH")]
        public static void TestCacheInvalidationForPost(string method)
        {
            const string relatedUrl = "http://api/SomeLocationUrl/";
            // setup
            var locationUrl = new Uri(relatedUrl);
            var request = new HttpRequestMessage(new HttpMethod(method), TestUrl);
            string routePattern = "http://myserver/api/stuffs/*";
            var entityTagStore = new Mock<IEntityTagStore>();

            var cachingHandler = new CachingHandler(new HttpConfiguration(), entityTagStore.Object)
            {

            };
            var entityTagKey = new CacheKey(TestUrl, new string[0], routePattern);
            var response = new HttpResponseMessage();
            response.Headers.Location = locationUrl;
            var invalidateCacheForPost = cachingHandler.PostInvalidationRule(entityTagKey, request, response);
            if (method == "POST")
            {
                entityTagStore.Expect(x => x.RemoveAllByRoutePatternAsync("/SomeLocationUrl/")).Returns(Task.FromResult(1));
            }

            // run
            invalidateCacheForPost();

            // verify



        }

        [Test]
        public void Test_NoStore_ResultsIn_ExpiredResourceAndPragmaNoCache()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, TestUrl);
            request.Headers.Add(HttpHeaderNames.Accept, "text/xml");
            var entityTagHeaderValue = new TimedEntityTagHeaderValue("\"12345678\"");
            var cachingHandler = new CachingHandler(new HttpConfiguration())
            {
                ETagValueGenerator = (x, y) => entityTagHeaderValue,
                CacheControlHeaderProvider = (r, c) =>
                                                 {
                                                     return new CacheControlHeaderValue()
                                                                {
                                                                    NoStore = true,
                                                                    NoCache = true
                                                                };
                                                 }
            };
            var response = request.CreateResponse(HttpStatusCode.Accepted);

            cachingHandler.AddCaching(new CacheKey(TestUrl, new string[0]), request, response)();
            Assert.IsTrue(response.Headers.Pragma.Any(x => x.Name == "no-cache"), "no-cache not in pragma");

        }

        [TestCase("GET", true, true, true, false, new[] { "Accept", "Accept-Language" })]
        [TestCase("GET", false, true, true, false, new[] { "Accept", "Accept-Language" })]
        [TestCase("PUT", false, true, false, false, new string[0])]
        [TestCase("PUT", false, false, true, true, new[] { "Accept" })]
        public static void AddCaching(string method,
            bool existsInStore,
            bool addVaryHeader,
            bool addLastModifiedHeader,
            bool alreadyHasLastModified,
            string[] varyByHeader)
        {
            // setup 
            var request = new HttpRequestMessage(new HttpMethod(method), TestUrl);
            request.Headers.Add(HttpHeaderNames.Accept, "text/xml");
            request.Headers.Add(HttpHeaderNames.AcceptLanguage, "en-GB");
            var entityTagStore = new Mock<IEntityTagStore>();
            var entityTagHeaderValue = new TimedEntityTagHeaderValue("\"12345678\"");
            var cachingHandler = new CachingHandler(new HttpConfiguration(), entityTagStore.Object, varyByHeader)
            {
                AddLastModifiedHeader = addLastModifiedHeader,
                AddVaryHeader = addVaryHeader,
                ETagValueGenerator = (x, y) => entityTagHeaderValue
            };


            var entityTagKey = new CacheKey(TestUrl, new[] { "text/xml", "en-GB" }, TestUrl + "/*");

            entityTagStore.Setup(x => x.GetValueAsync(It.Is<CacheKey>(etg => etg.ResourceUri == TestUrl)))
                .Returns(Task.FromResult(existsInStore ? entityTagHeaderValue : null));

            if (!existsInStore)
            {
                entityTagStore.Setup(
                    x => x.AddOrUpdateAsync(It.Is<CacheKey>(etk => etk == entityTagKey),
                        It.Is<TimedEntityTagHeaderValue>(ethv => ethv.Tag == entityTagHeaderValue.Tag)))
                        .Returns(Task.FromResult(0));
            }

            var response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(new byte[0]);
            if (alreadyHasLastModified)
                response.Content.Headers.Add(HttpHeaderNames.LastModified, DateTimeOffset.Now.ToString("r"));

            var cachingContinuation = cachingHandler.AddCaching(entityTagKey, request, response);

            // run
            cachingContinuation();

            // verify

            // test kast modified only if it is GET and PUT
            if (addLastModifiedHeader && method.IsIn("PUT", "GET"))
            {
                Assert.That(response.Content.Headers.Any(x => x.Key == HttpHeaderNames.LastModified),
                    "LastModified does not exist");
            }
            if (!addLastModifiedHeader && !alreadyHasLastModified)
            {
                Assert.That(!response.Content.Headers.Any(x => x.Key == HttpHeaderNames.LastModified),
                    "LastModified exists");
            }

        }

        [TestCase("DELETE")]
        [TestCase("PUT")]
        [TestCase("POST")]
        [TestCase("PATCH")]
        public static void TestManualInvalidation(string method)
        {
            // setup

            var entityTagStore = new Mock<IEntityTagStore>();
            var cachingHandler = new CachingHandler(new HttpConfiguration(), entityTagStore.Object)
            {

            };

            entityTagStore.Setup(x => x.RemoveResourceAsync("/api/stuff/")).Returns(Task.FromResult(1));
            entityTagStore.Setup(x => x.RemoveResourceAsync("/api/more/")).Returns(Task.FromResult(1));


            // run
            cachingHandler.InvalidateResourceAsync(new HttpRequestMessage(new HttpMethod(method), new Uri(TestUrl))).Wait();
            cachingHandler.InvalidateResourceAsync(new HttpRequestMessage(new HttpMethod(method), new Uri(TestUrl2))).Wait();

            // verify
        }



    }
}
