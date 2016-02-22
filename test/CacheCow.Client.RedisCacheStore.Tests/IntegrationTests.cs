using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Common;
using NUnit.Framework;
using CacheCow.Client.Headers;
using CacheCow.Client.RedisCacheStore;

namespace CacheCow.Client.RedisCacheStore.Tests
{

	/// <summary>
	/// READ!! ------------------
	/// These tests require Redis running on localhost and access to internet
	/// </summary>
	[TestFixture]
	public class IntegrationTests
	{

		private const string CacheableResource1 = "https://ajax.googleapis.com/ajax/libs/jquery/1.8.2/jquery.min.js";
		private const string CacheableResource2 = "http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.8.2.min.js";
	    private const string ConnectionString = "localhost";

		[Ignore]
		[Test]
		public void TestConnectivity()
		{
            var redisStore = new RedisStore(ConnectionString);
			HttpResponseMessage responseMessage = null;
			Console.WriteLine(redisStore.GetValueAsync(new CacheKey("http://google.com", new string[0])).Result);
		}

		[Ignore]
		[Test]
		public void AddItemTest()
		{
            var client = new HttpClient(new CachingHandler(new RedisStore(ConnectionString))
			               	{
			               		InnerHandler = new HttpClientHandler()
			               	});

			var httpResponseMessage = client.GetAsync(CacheableResource1).Result;
			var httpResponseMessage2 = client.GetAsync(CacheableResource1).Result;
			Assert.That(httpResponseMessage2.Headers.GetCacheCowHeader().RetrievedFromCache.Value);
		}

		[Ignore]
		[Test]
		public void GetValue()
		{
            var redisStore = new RedisStore(ConnectionString);
			var client = new HttpClient(new CachingHandler(redisStore)
			{
				InnerHandler = new HttpClientHandler()
			});

			var httpResponseMessage = client.GetAsync(CacheableResource1).Result;
			var response = redisStore.GetValueAsync(new CacheKey(CacheableResource1, new string[0])).Result;
			Assert.IsNotNull(response);

		}
	}
}
