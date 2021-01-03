using System;
using System.Net.Http;
using System.Net.Http.Headers;
using CacheCow.Client.Tests.Helper;
using Xunit;

namespace CacheCow.Client.Tests
{
    public class StalenessTests
    {
        [Fact]
        public void TypicalStaleCase_Expires()
        {
            var expired = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(10));
            var cachedResponse = ResponseHelper.GetOkMessage();
            cachedResponse.Content = new StringContent("Ravi");
            cachedResponse.Content.Headers.Expires = expired;

            var req = new HttpRequestMessage(HttpMethod.Get, "http://some.sing");
            var freshness = CachingHandler.IsFreshOrStaleAcceptable(cachedResponse, req);

            Assert.NotNull(freshness);
            Assert.True(freshness);
        }

        [Fact]
        public void TypicalFreshCase_Expires()
        {
            var fresh = DateTimeOffset.Now.Add(TimeSpan.FromMinutes(10));
            var cachedResponse = ResponseHelper.GetOkMessage();
            cachedResponse.Content = new StringContent("Ravi");
            cachedResponse.Content.Headers.Expires = fresh;

            var req = new HttpRequestMessage(HttpMethod.Get, "http://some.sing");
            var freshness = CachingHandler.IsFreshOrStaleAcceptable(cachedResponse, req);

            Assert.NotNull(freshness);
            Assert.True(freshness);
        }

        [Fact]
        public void TypicalFreshCase_MaxAge()
        {
            var cachedResponse = ResponseHelper.GetOkMessage();
            cachedResponse.Content = new StringContent("Ravi");
            cachedResponse.Headers.CacheControl = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(1)
            };

            var req = new HttpRequestMessage(HttpMethod.Get, "http://some.sing");
            var freshness = CachingHandler.IsFreshOrStaleAcceptable(cachedResponse, req);

            Assert.NotNull(freshness);
            Assert.True(freshness);
        }

        [Fact] public void TypicalStaleCase_MaxAge()
        {
            var cachedResponse = ResponseHelper.GetOkMessage();
            cachedResponse.Content = new StringContent("Ravi");
            cachedResponse.Headers.CacheControl = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(1)
            };

            cachedResponse.Headers.Date = DateTimeOffset.Now.Subtract(TimeSpan.FromSeconds(10));

            var req = new HttpRequestMessage(HttpMethod.Get, "http://some.sing");
            var freshness = CachingHandler.IsFreshOrStaleAcceptable(cachedResponse, req);

            Assert.NotNull(freshness);
            Assert.False(freshness);
        }

        [Fact] public void TypicalStaleCase_MaxAge_and_Age()
        {
            var cachedResponse = ResponseHelper.GetOkMessage();
            cachedResponse.Content = new StringContent("Ravi");
            cachedResponse.Headers.CacheControl = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(60)
            };
            cachedResponse.Headers.Age = TimeSpan.FromSeconds(50);
            cachedResponse.Headers.Date = DateTimeOffset.Now.Subtract(TimeSpan.FromSeconds(50));


            var req = new HttpRequestMessage(HttpMethod.Get, "http://some.sing");
            var freshness = CachingHandler.IsFreshOrStaleAcceptable(cachedResponse, req);

            Assert.NotNull(freshness);
            Assert.False(freshness);
        }

        [Fact] public void TypicalFreshCase_MaxAge_and_Age()
        {
            var cachedResponse = ResponseHelper.GetOkMessage();
            cachedResponse.Content = new StringContent("Ravi");
            cachedResponse.Headers.CacheControl = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(60)
            };
            cachedResponse.Headers.Age = TimeSpan.FromSeconds(5);
            cachedResponse.Headers.Date = DateTimeOffset.Now.Subtract(TimeSpan.FromSeconds(50));


            var req = new HttpRequestMessage(HttpMethod.Get, "http://some.sing");
            var freshness = CachingHandler.IsFreshOrStaleAcceptable(cachedResponse, req);

            Assert.NotNull(freshness);
            Assert.True(freshness);
        }


    }
}
