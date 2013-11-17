using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CacheCow.Common;
using Enyim.Caching.Configuration;
using NUnit.Framework;

namespace CacheCow.Server.EntityTagStore.Memcached.Tests
{

    [TestFixture]
    public class IntegrationTests
    {

        private MemcachedEntityTagStore _memcachedEntityTagStore = null;
        private const int PortNumber = 11311;


        [TearDown]
        public void TearDown()
        {
           
            if(_memcachedEntityTagStore!=null)
                _memcachedEntityTagStore.Dispose();

        }

        [SetUp]
        public void Setup()
        {
            var configuration = new MemcachedClientConfiguration();
            configuration.AddServer("127.0.0.1", PortNumber);
            _memcachedEntityTagStore = new MemcachedEntityTagStore(configuration);
            _memcachedEntityTagStore.Clear();
        }

        [Ignore]
        [Test]
        public void CheckStorage()
        {
            var cacheKey = GetCacheKey();
            var original = GetETag();
            _memcachedEntityTagStore.AddOrUpdate(cacheKey, original);

            TimedEntityTagHeaderValue etag = null;
            Assert.IsTrue(_memcachedEntityTagStore.TryGetValue(cacheKey, out etag), "retrieving failed!!");

            Assert.AreEqual(original.Tag, etag.Tag);

        }

        [Ignore]
        [Test]
        public void RemoveByRoutePatternTest()
        {
            var cacheKey = GetCacheKey();
            var original = GetETag();
            _memcachedEntityTagStore.AddOrUpdate(cacheKey, original);


            var removeAllByRoutePattern = _memcachedEntityTagStore.RemoveAllByRoutePattern(cacheKey.RoutePattern);
            TimedEntityTagHeaderValue etag = null;
            Assert.IsFalse(_memcachedEntityTagStore.TryGetValue(cacheKey, out etag), "retrieving failed!!");

            Assert.IsNull(etag);

        }

        [Ignore]
        [Test]
        public void RemoveByResourceUriTest()
        {
            var cacheKey = GetCacheKey(true);
            var cacheKey2 = GetCacheKey(true);
            var original = GetETag();
            _memcachedEntityTagStore.AddOrUpdate(cacheKey, original);
            _memcachedEntityTagStore.AddOrUpdate(cacheKey2, original);

            var result = _memcachedEntityTagStore.RemoveResource(cacheKey.ResourceUri);
            Assert.AreEqual(2, result);

        }

        private CacheKey GetCacheKey(bool differentRoutPattern = false)
        {
            const string url = "/api/1/2/3";
            return new CacheKey(url, new string[]{Guid.NewGuid().ToString()},
                differentRoutPattern ? 
                url + Guid.NewGuid().ToString() :
                url
            );
        }

        private TimedEntityTagHeaderValue GetETag()
        {
            return new TimedEntityTagHeaderValue("\"67137891238912\"") {LastModified = DateTime.Now};
        }



    }
}
