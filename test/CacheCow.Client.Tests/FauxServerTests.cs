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

        [SetUp]
        public void Setup()
        {
            var _cachingHandler = new CachingHandler()
            {
                InnerHandler = _dummyHandler
            };

            _httpClient = new HttpClient(_cachingHandler);
        }

        [Test]
        public void RespectsExpiry()
        {
            _dummyHandler.Response = GetOkMessage(1, true);
            var response = _httpClient.GetAsync(DummyUrl).Result;
            Thread.Sleep(1500);
            _dummyHandler.Response = GetNotModifiedMessage(1);

            // first caching
            response = _httpClient.GetAsync(DummyUrl).Result;
            Console.WriteLine(response.Headers.GetCacheCowHeader().ToString());
            Assert.IsTrue(response.Headers.GetCacheCowHeader().WasStale.Value);

            // stale go getter
            Thread.Sleep(1500);
            response = _httpClient.GetAsync(DummyUrl).Result;
            Console.WriteLine(response.Headers.GetCacheCowHeader().ToString());
            Assert.IsTrue(response.Headers.GetCacheCowHeader().WasStale.Value);

            // immediate, get it from cache - short circuit
            response = _httpClient.GetAsync(DummyUrl).Result;
            Console.WriteLine(response.Headers.GetCacheCowHeader().ToString());
            Assert.IsNull(response.Headers.GetCacheCowHeader().WasStale);
        }


        private HttpResponseMessage GetOkMessage(int expirySeconds = 200, bool mustRevalidate = false)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(expirySeconds),
                MustRevalidate = mustRevalidate
            };
            response.Headers.Date = DateTimeOffset.UtcNow;
            response.Content = new ByteArrayContent(new byte[256]);
            return response;
        }

        private HttpResponseMessage GetNotModifiedMessage(int expirySeconds = 200)
        {
            var response = new HttpResponseMessage(HttpStatusCode.NotModified);
            response.Headers.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(expirySeconds),
                MustRevalidate = true
            };
            response.Headers.Date = DateTimeOffset.UtcNow;
            return response;
        }
    }
}
