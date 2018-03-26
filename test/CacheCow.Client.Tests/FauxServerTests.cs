using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using CacheCow.Client.Headers;
using CacheCow.Client.Tests.Helper;

namespace CacheCow.Client.Tests
{
    /// <summary>
    /// Difference of these tests is that it uses in-memory message handler
    /// </summary>
    public class FauxServerTests
    {       
        private DummyMessageHandler _dummyHandler = new DummyMessageHandler();
        private HttpClient _httpClient;
        private const string DummyUrl = "http://myserver/api/dummy";
        private const string ETagValue = "\"abcdef\"";
        private InMemoryCacheStore _store = new InMemoryCacheStore();

        [SetUp]
        public void Setup()
        {

            var _cachingHandler = new CachingHandler(_store)
            {
                InnerHandler = _dummyHandler
            };

            _httpClient = new HttpClient(_cachingHandler);
        }

        [Test]
        public void RespectsExpiry()
        {
            _dummyHandler.Response = ResponseHelper.GetOkMessage(1, true);
            var response = _httpClient.GetAsync(DummyUrl).Result;
            Thread.Sleep(100);
            _dummyHandler.Response = ResponseHelper.GetNotModifiedMessage(1);

            // first caching
            response = _httpClient.GetAsync(DummyUrl).Result;
            Console.WriteLine(response.Headers.GetCacheCowHeader().ToString());
            Assert.NotNull(response.Headers.GetCacheCowHeader().RetrievedFromCache, "RetrievedFromCache");
            Assert.IsTrue(response.Headers.GetCacheCowHeader().RetrievedFromCache.Value);

            // stale go getter
            Thread.Sleep(1500);
            _dummyHandler.Response = ResponseHelper.GetOkMessage(1, true);
            response = _httpClient.GetAsync(DummyUrl).Result;
            Console.WriteLine(response.Headers.GetCacheCowHeader().ToString());
            Assert.NotNull(response.Headers.GetCacheCowHeader().DidNotExist, "DidNotExist");
            Assert.IsTrue(response.Headers.GetCacheCowHeader().DidNotExist.Value);

            // immediate, get it from cache - short circuit
            response = _httpClient.GetAsync(DummyUrl).Result;
            Console.WriteLine(response.Headers.GetCacheCowHeader().ToString());
            Assert.IsNull(response.Headers.GetCacheCowHeader().WasStale);
        }

        [Test]
        public void ContentGetsSerializedCorrectly()
        {
            _dummyHandler.Response = ResponseHelper.GetOkMessage(2000, true);

            for (int i = 0; i < 1000; i++)
            {
                // first caching
                string url = DummyUrl + Guid.NewGuid().ToString();
                var response = _httpClient.GetAsync(url).Result;
                _dummyHandler.Response = ResponseHelper.GetOkMessage();

                // read from cache
                response = _httpClient.GetAsync(url).Result;

                Assert.AreEqual(ResponseHelper.ContentString, response.Content.ReadAsStringAsync().Result);
            }
        }
    }
}
