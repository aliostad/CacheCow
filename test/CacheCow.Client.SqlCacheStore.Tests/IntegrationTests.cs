using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Common;
using NUnit;
using NUnit.Framework;

namespace CacheCow.Client.SqlCacheStore.Tests
{
    [TestFixture]
    public class IntegrationTests
    {
        [Ignore]
        [Test]
        public void TestClear()
        {
            var sqlStore = new SqlStore();
            sqlStore.Clear();
        }

        [Ignore]
        [Test]
        public void FullTests()
        {
            var cacheKey = new CacheKey("http://google.com", new string[0]);
            var sqlStore = new SqlStore();
            var httpClient = new HttpClient();
            sqlStore.AddOrUpdate(cacheKey,
                httpClient.GetAsync("http://google.com").Result
                );

            HttpResponseMessage responseMessage;

            Assert.IsTrue(sqlStore.TryGetValue(cacheKey, out responseMessage), "TryGetValue returned false");
            Assert.IsNotNull(responseMessage, "responseMessage null");
            Assert.IsTrue(responseMessage.Headers.Count() > 0);
            Assert.IsTrue(sqlStore.TryRemove(cacheKey));
        }

        [Ignore]
        [Test]
        public void GetDomains()
        {
            var cacheKey = new CacheKey("http://google.com", new string[] {"Accept"});
            var cacheKey2 = new CacheKey("http://facebook.com", new string[] {"Accept"}

    );
            var sqlStore = new SqlStore();
            var httpClient = new HttpClient();
            sqlStore.AddOrUpdate(cacheKey,
                httpClient.GetAsync("http://google.com").Result
                );
            sqlStore.AddOrUpdate(cacheKey2,
                httpClient.GetAsync("http://facebook.com").Result
                );


            var domainSizes = sqlStore.GetDomainSizes();
            Assert.AreEqual(2, domainSizes.Count);
        }

    }
}
