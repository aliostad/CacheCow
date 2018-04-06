using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using CacheCow.Client.Headers;
using System.Net;
using CacheCow.Client;
using System.Threading;

namespace CacheCow.Server.Core.Mvc.Tests
{
    public class CacheResourceTests
    {
        private TestServer _server;
        private HttpClient _client;

        public CacheResourceTests()
        {
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<HttpCacheFilterTestsStartup>());
            _client = _server.CreateClient();
        }

        [Fact]
        public async Task ForSimpleGetYouGetCacheControlAndBack()
        {
            var response = await _client.GetAsync("/api/bettertest/1");
            var viewModel = await response.Content.ReadAsAsync<TestViewModel>();
            Assert.NotNull(response.Content);
            Assert.NotNull(response.Content.Headers.LastModified);
        }

        [Fact]
        public async Task WithClientCachingReturnsFromCacheSecondRequestIfExpiryNotPassed()
        {
            var handler = _server.CreateHandler();
            var client = ClientExtensions.CreateClient(handler);
            client.BaseAddress = _server.BaseAddress;
            var response = await client.GetAsync("/api/bettertest/1");
            var response2 = await client.GetAsync("/api/bettertest/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response2.Headers.GetCacheCowHeader().RetrievedFromCache);
        }

        [Fact]
        public async Task WithClientCachingReturnsFromCacheSecondRequestIfExpiryZero()
        {
            var handler = _server.CreateHandler();
            var client = ClientExtensions.CreateClient(handler);
            client.BaseAddress = _server.BaseAddress;
            var response = await client.GetAsync("/api/bettertest/");
            Thread.Sleep(1000);
            var response2 = await client.GetAsync("/api/bettertest/");
            var cch = response2.Headers.GetCacheCowHeader();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(cch.CacheValidationApplied);
            Assert.True(cch.RetrievedFromCache);
        }

        [Fact]
        public async Task HasEitherLastModifiedOrETag()
        {
            var response = await _client.GetAsync("/api/bettertest/1");
            var response2 = await _client.GetAsync("/api/bettertest/1");
            Assert.NotNull((object) response.Headers.ETag ?? response.Content.Headers.LastModified);
        }
    }

}
