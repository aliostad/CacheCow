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
                store.AddOrUpdateAsync(cacheKey, headerValue).Wait();
                TimedEntityTagHeaderValue storedHeader;
                Assert.NotNull(storedHeader = store.GetValueAsync(cacheKey).Result);
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
                store.AddOrUpdateAsync(cacheKey, headerValue).Wait();
                store.TryRemoveAsync(cacheKey).Wait();
                TimedEntityTagHeaderValue storedHeader;
                storedHeader = store.GetValueAsync(cacheKey).Result;
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
                store.AddOrUpdateAsync(cacheKey, headerValue).Wait();
                store.AddOrUpdateAsync(cacheKey2, headerValue).Wait();
                store.RemoveAllByRoutePatternAsync(RoutePattern).Wait();
                store.TryRemoveAsync(cacheKey).Wait();
                Assert.Null(store.GetValueAsync(cacheKey).Result);
                Assert.Null(store.GetValueAsync(cacheKey2).Result);
            }
        }

    }
}
