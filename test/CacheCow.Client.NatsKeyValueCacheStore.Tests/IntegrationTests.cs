using System;
using System.Net;
using Xunit;
using NATS.Client;
using CacheCow.Client.Headers;
using CacheCow.Common;

namespace CacheCow.Client.NatsKeyValueCacheStore.Tests
{
    public class IntegrationTests
    {

        private const string CacheableResource1 = "https://ajax.googleapis.com/ajax/libs/jquery/1.8.2/jquery.min.js";
        private const string CacheableResource2 = "http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.8.2.min.js";
        private const string ConnectionString = "nats://0.0.0.0:59039";
        private const string MaxAgeZeroResource = "https://google.com/";
        private const string BucketName = "vavavoom";
        private const string SkipText = "Run manually"; // NOTE: Change to NULL to run the tests and the back to "Run manually"


        private Options _options;

        public IntegrationTests()
        {
            _options = ConnectionFactory.GetDefaultOptions();
            _options.Url = ConnectionString;
            _options.User = "local";
            _options.Password = "ZcQOmbdc0GuZEsRwvgfWKGAPVtOhjHTc"; // CHANGE ACCORDING TO YOUR ENVIRONMENT
        }

        [Fact(Skip = SkipText)]
        public async void AddItemTest()
        {
            var client = new HttpClient(new CachingHandler(new NatsKeyValueStore(BucketName, _options))
            {
                InnerHandler = new HttpClientHandler()
            });

            var httpResponseMessage = await client.GetAsync(CacheableResource1);
            var httpResponseMessage2 = await client.GetAsync(CacheableResource1);
            Assert.True(httpResponseMessage2.Headers.GetCacheCowHeader().RetrievedFromCache.Value);
        }

        [Fact(Skip = SkipText)]
        public async void ExceptionTest()
        {
            var client = new HttpClient(new CachingHandler(new NatsKeyValueStore(BucketName, _options))
            {
                InnerHandler = new HttpClientHandler()
            });

            var httpResponseMessage = await client.GetAsync(CacheableResource1);
            var httpResponseMessage2 = await client.GetAsync(CacheableResource1);
            Assert.Equal(HttpStatusCode.OK, httpResponseMessage.StatusCode);
            Assert.Equal(HttpStatusCode.OK, httpResponseMessage2.StatusCode);
        }

        [Fact(Skip = SkipText)]
        public async void GetValue()
        {
            var redisStore = new NatsKeyValueStore(BucketName, _options);
            var client = new HttpClient(new CachingHandler(redisStore)
            {
                InnerHandler = new HttpClientHandler()
            });

            var httpResponseMessage = await client.GetAsync(CacheableResource1);
            var response = redisStore.GetValueAsync(new CacheKey(CacheableResource1, new string[0])).Result;
            Assert.NotNull(response);
        }

        [Fact(Skip = SkipText)]
        public void TestConnectivity()
        {
            var redisStore = new NatsKeyValueStore(BucketName, _options);
            HttpResponseMessage responseMessage = null;
            Console.WriteLine(redisStore.GetValueAsync(new CacheKey("http://google.com", new string[0])).Result);
        }

        [Fact(Skip = SkipText)]
        public void WorksWithMaxAgeZeroAndStillStoresIt()
        {
            var redisStore = new NatsKeyValueStore(BucketName, _options);
            var client = new HttpClient(new CachingHandler(redisStore)
            {
                InnerHandler = new HttpClientHandler(),
                DefaultVaryHeaders = new string[0]
            });

            var httpResponseMessage = client.GetAsync(MaxAgeZeroResource).Result;
            var key = new CacheKey(MaxAgeZeroResource, new string[0]);
            var response = redisStore.GetValueAsync(key).Result;
            Assert.NotNull(response);
        }
    }
}


