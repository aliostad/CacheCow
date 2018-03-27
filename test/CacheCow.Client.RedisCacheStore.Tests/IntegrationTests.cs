using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using CacheCow.Common;
using Xunit;
using CacheCow.Client.Headers;
using CacheCow.Client.RedisCacheStore;

namespace CacheCow.Client.RedisCacheStore.Tests
{

	/// <summary>
	/// READ!! ------------------
	/// These tests require Redis running on localhost and access to internet
	/// </summary>
	
	public class IntegrationTests
	{

		private const string CacheableResource1 = "https://ajax.googleapis.com/ajax/libs/jquery/1.8.2/jquery.min.js";
		private const string CacheableResource2 = "http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.8.2.min.js";
        private const string MaxAgeZeroResource = "https://google.com/";
        private const string ConnectionString = "localhost";

        [Fact(Skip = "Run manually")]
        public void TestConnectivity()
		{
            var redisStore = new RedisStore(ConnectionString);
			HttpResponseMessage responseMessage = null;
			Console.WriteLine(redisStore.GetValueAsync(new CacheKey("http://google.com", new string[0])).Result);
		}

        [Fact(Skip = "Run manually")]
        public void AddItemTest()
		{
            var client = new HttpClient(new CachingHandler(new RedisStore(ConnectionString))
			               	{
			               		InnerHandler = new HttpClientHandler()
			               	});

			var httpResponseMessage = client.GetAsync(CacheableResource1).Result;
			var httpResponseMessage2 = client.GetAsync(CacheableResource1).Result;
			Assert.True(httpResponseMessage2.Headers.GetCacheCowHeader().RetrievedFromCache.Value);
		}

        [Fact(Skip = "Run manually")]
        public void ExceptionTest()
        {
            var client = new HttpClient(new CachingHandler(new RedisStore(ConnectionString, throwExceptions: false))
            {
                InnerHandler = new HttpClientHandler()
            });

            var httpResponseMessage = client.GetAsync(CacheableResource1).Result;
            var httpResponseMessage2 = client.GetAsync(CacheableResource1).Result;
            Assert.Equal(HttpStatusCode.OK, httpResponseMessage.StatusCode);
            Assert.Equal(HttpStatusCode.OK, httpResponseMessage2.StatusCode);
        }

        [Fact(Skip = "Run manually")]
        public void GetValue()
		{
            var redisStore = new RedisStore(ConnectionString);
			var client = new HttpClient(new CachingHandler(redisStore)
			{
				InnerHandler = new HttpClientHandler()
			});

			var httpResponseMessage = client.GetAsync(CacheableResource1).Result;
			var response = redisStore.GetValueAsync(new CacheKey(CacheableResource1, new string[0])).Result;
			Assert.NotNull(response);

		}

        [Fact(Skip = "Run manually")]
        public void WorksWithMaxAgeZeroAndStillStoresIt()
        {
            var redisStore = new RedisStore(ConnectionString);
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
