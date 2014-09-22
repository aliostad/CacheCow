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
            _entityTagStore.RemoveResource(GetCacheKey().ResourceUri);
            _entityTagStore.RemoveAllByRoutePattern(GetCacheKey().RoutePattern);
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
            _entityTagStore.AddOrUpdate(cacheKey, original);

            TimedEntityTagHeaderValue etag = null;
            Assert.IsTrue(_entityTagStore.TryGetValue(cacheKey, out etag), "retrieving failed!!");

            Assert.AreEqual(original.Tag, etag.Tag);

        }

        [Ignore]
        [Test]
        public void RemoveByRoutePatternTest()
        {
            var cacheKey = GetCacheKey();
            var original = GetETag();
            _entityTagStore.AddOrUpdate(cacheKey, original);


            var removeAllByRoutePattern = _entityTagStore.RemoveAllByRoutePattern(cacheKey.RoutePattern);
            TimedEntityTagHeaderValue etag = null;
            Assert.IsFalse(_entityTagStore.TryGetValue(cacheKey, out etag), "retrieving failed!!");

            Assert.IsNull(etag);

        }

        [Ignore]
        [Test]
        public void RemoveByResourceUriTest()
        {
            var cacheKey = GetCacheKey(true);
            var cacheKey2 = GetCacheKey(true);
            var original = GetETag();
            _entityTagStore.AddOrUpdate(cacheKey, original);
            _entityTagStore.AddOrUpdate(cacheKey2, original);

            var result = _entityTagStore.RemoveResource(cacheKey.ResourceUri);
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
