using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CacheCow.Common;
using CacheCow.Server;
using NUnit.Framework;

namespace CacheCow.Tests.Server
{
    [TestFixture]
    public class InMemoryEntityTagStoreTests
    {

        private const string Url = "http://www.amazon.com/Pro-ASP-NET-Web-API-Services/dp/1430247258";

        [Test]
        public void AddGetTest()
        {
            using (var store = new InMemoryEntityTagStore())
            {
                var cacheKey = new CacheKey(Url, new[] { "Accept" });

                var headerValue = new TimedEntityTagHeaderValue("\"abcdefghijkl\"");
                store.AddOrUpdate(cacheKey, headerValue);
                TimedEntityTagHeaderValue storedHeader;
                Assert.True(store.TryGetValue(cacheKey, out storedHeader));
                Assert.AreEqual(headerValue.ToString(), storedHeader.ToString());                
            }
        }



        [Test]
        public void AddRemoveTest()
        {
            using (var store = new InMemoryEntityTagStore())
            {
                var cacheKey = new CacheKey(Url, new[] { "Accept" });
                var headerValue = new TimedEntityTagHeaderValue("\"abcdefghijkl\"");
                store.AddOrUpdate(cacheKey, headerValue);
                store.TryRemove(cacheKey);
                TimedEntityTagHeaderValue storedHeader;
                Assert.False(store.TryGetValue(cacheKey, out storedHeader));
                Assert.IsNull(storedHeader);
            }
        }

        [Test]
        public void AddRemoveByPatternTest()
        {

            const string RoutePattern = "stuff";

            using (var store = new InMemoryEntityTagStore())
            {
                var cacheKey = new CacheKey(Url, new[] { "Accept" }, RoutePattern);
                var cacheKey2 = new CacheKey(Url + "/chaja", new[] { "Accept" }, RoutePattern);
                var headerValue = new TimedEntityTagHeaderValue("\"abcdefghijkl\"");
                store.AddOrUpdate(cacheKey, headerValue);
                store.AddOrUpdate(cacheKey2, headerValue);
                store.RemoveAllByRoutePattern(RoutePattern);
                store.TryRemove(cacheKey);
                TimedEntityTagHeaderValue storedHeader;
                Assert.False(store.TryGetValue(cacheKey, out storedHeader));
                Assert.False(store.TryGetValue(cacheKey2, out storedHeader));
                Assert.IsNull(storedHeader);
            }
        }

    }
}
