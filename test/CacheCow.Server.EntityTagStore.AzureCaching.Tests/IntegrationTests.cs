namespace CacheCow.Server.EntityTagStore.AzureCaching.Tests
{
	using System;

	using CacheCow.Common;

	using NUnit.Framework;

	[TestFixture]
	public class IntegrationTests
	{
		private AzureCachingEntityTagStore azureCacheEntityTagStore = null;

		[TearDown]
		public void TearDown()
		{
		}

		[SetUp]
		public void Setup()
		{
			this.azureCacheEntityTagStore = new AzureCachingEntityTagStore();
			this.azureCacheEntityTagStore.Clear();
		}

		[Ignore]
		[Test]
		public void CheckStorage()
		{
			var cacheKey = GetCacheKey();
			var original = GetETag();
			azureCacheEntityTagStore.AddOrUpdate(cacheKey, original);

			TimedEntityTagHeaderValue etag = null;
			Assert.IsTrue(azureCacheEntityTagStore.TryGetValue(cacheKey, out etag), "retrieving failed!!");

			Assert.AreEqual(original.Tag, etag.Tag);
		}

		[Ignore]
		[Test]
		public void RemoveByRoutePatternTest()
		{
			var cacheKey = GetCacheKey();
			var original = GetETag();
			azureCacheEntityTagStore.AddOrUpdate(cacheKey, original);


			int removeAllByRoutePattern = azureCacheEntityTagStore.RemoveAllByRoutePattern(cacheKey.RoutePattern);
			TimedEntityTagHeaderValue etag = null;
			Assert.IsFalse(azureCacheEntityTagStore.TryGetValue(cacheKey, out etag), "retrieving failed!!");

			Assert.IsNull(etag);

		}

		[Ignore]
		[Test]
		public void RemoveByResourceUriTest()
		{
			var cacheKey = GetCacheKey(true);
			var cacheKey2 = GetCacheKey(true);
			var original = GetETag();
			azureCacheEntityTagStore.AddOrUpdate(cacheKey, original);
			azureCacheEntityTagStore.AddOrUpdate(cacheKey2, original);

			var result = azureCacheEntityTagStore.RemoveResource(cacheKey.ResourceUri);
			Assert.AreEqual(2, result);
		}

		private CacheKey GetCacheKey(bool differentRoutPattern = false)
		{
			const string url = "/api/1/2/3";
			return new CacheKey(url, new string[] { Guid.NewGuid().ToString() },
				differentRoutPattern ?
				url + Guid.NewGuid().ToString() :
				url
			);
		}

		private TimedEntityTagHeaderValue GetETag()
		{
			return new TimedEntityTagHeaderValue("\"67137891238912\"") { LastModified = DateTime.Now };
		}
	}
}