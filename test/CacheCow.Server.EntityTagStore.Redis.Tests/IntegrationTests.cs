using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Common;
using NUnit.Framework;

namespace CacheCow.Server.EntityTagStore.Redis.Tests
{
    /// <summary>
    /// Assuming local server running on default port
    /// </summary>
    /// 
    [TestFixture]
    public class IntegrationTests
    {
        private RedisEntityTagStore _entityTagStore;

        [TearDown]
        public void TearDown()
        {
            _entityTagStore.RemoveResourceAsync(GetCacheKey().ResourceUri).Wait();
            _entityTagStore.RemoveAllByRoutePatternAsync(GetCacheKey().RoutePattern).Wait();
        }

        [SetUp]
        public void Setup()
        {
            _entityTagStore = new RedisEntityTagStore("localhost");
        }

        [Ignore]
        [Test]
        public void CheckStorage()
        {
            var cacheKey = GetCacheKey();
            var original = GetETag();
            _entityTagStore.AddOrUpdateAsync(cacheKey, original).Wait();

            TimedEntityTagHeaderValue etag = null;
            Assert.IsTrue( (etag = _entityTagStore.GetValueAsync(cacheKey).Result) != null, "retrieving failed!!");

            Assert.AreEqual(original.Tag, etag.Tag);
        }

        [Ignore]
        [Test]
        public void RemoveByRoutePatternTest()
        {
            var cacheKey = GetCacheKey();
            var original = GetETag();
            _entityTagStore.AddOrUpdateAsync(cacheKey, original).Wait();

            var removeAllByRoutePattern = _entityTagStore.RemoveAllByRoutePatternAsync(cacheKey.RoutePattern).Result;
            TimedEntityTagHeaderValue etag = null;
            Assert.IsFalse( (etag = _entityTagStore.GetValueAsync(cacheKey).Result) != null, "retrieving failed!!");

            Assert.IsNull(etag);

        }

        [Ignore]
        [Test]
        public void RemoveByResourceUriTest()
        {
            var cacheKey = GetCacheKey(true);
            var cacheKey2 = GetCacheKey(true);
            var original = GetETag();
            _entityTagStore.AddOrUpdateAsync(cacheKey, original).Wait();
            _entityTagStore.AddOrUpdateAsync(cacheKey2, original).Wait();

            var result = _entityTagStore.RemoveResourceAsync(cacheKey.ResourceUri).Result;
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
            return new TimedEntityTagHeaderValue("\"" + Guid.NewGuid().ToString("N") + "\"") { LastModified = DateTime.Now };
        }
    }

       
}
