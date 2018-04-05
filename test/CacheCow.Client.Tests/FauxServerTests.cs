using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
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

        public FauxServerTests()
        {

            var _cachingHandler = new CachingHandler(_store)
            {
                InnerHandler = _dummyHandler
            };

            _httpClient = new HttpClient(_cachingHandler);
        }

        [Fact]
        public async Task RespectsExpiry()
        {
            _store = new InMemoryCacheStore(TimeSpan.Zero);
            var _cachingHandler = new CachingHandler(_store)
            {
                InnerHandler = _dummyHandler
            };

            _httpClient = new HttpClient(_cachingHandler);

            _dummyHandler.Response = ResponseHelper.GetOkMessage(1, true);
            var response = await _httpClient.GetAsync(DummyUrl);
            Thread.Sleep(100);
            _dummyHandler.Response = ResponseHelper.GetNotModifiedMessage(1);

            // first caching
            response = await _httpClient.GetAsync(DummyUrl);
            Console.WriteLine(response.Headers.GetCacheCowHeader().ToString());
            Assert.NotNull(response.Headers.GetCacheCowHeader().RetrievedFromCache);
            Assert.True(response.Headers.GetCacheCowHeader().RetrievedFromCache.Value);

            // stale go getter
            Thread.Sleep(1500);
            _dummyHandler.Response = ResponseHelper.GetOkMessage(1, true);
            response = await _httpClient.GetAsync(DummyUrl);
            Console.WriteLine(response.Headers.GetCacheCowHeader().ToString());
            Assert.NotNull(response.Headers.GetCacheCowHeader().DidNotExist);
            Assert.True(response.Headers.GetCacheCowHeader().DidNotExist.Value);

            // immediate, get it from cache - short circuit
            response = await _httpClient.GetAsync(DummyUrl);
            Console.WriteLine(response.Headers.GetCacheCowHeader().ToString());
            Assert.Null(response.Headers.GetCacheCowHeader().WasStale);
        }

        [Fact]
        public async Task ContentGetsSerializedCorrectly()
        {
            _dummyHandler.Response = ResponseHelper.GetOkMessage(2000, true);

            for (int i = 0; i < 1000; i++)
            {
                // first caching
                string url = DummyUrl + Guid.NewGuid().ToString();
                var response = await _httpClient.GetAsync(url);
                _dummyHandler.Response = ResponseHelper.GetOkMessage();

                // read from cache
                response = await _httpClient.GetAsync(url);

                Assert.Equal(ResponseHelper.ContentString, await response.Content.ReadAsStringAsync());
            }
        }
    }
}
