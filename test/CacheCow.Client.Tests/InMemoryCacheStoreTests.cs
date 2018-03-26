using CacheCow.Client.Tests.Helper;
using CacheCow.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CacheCow.Client.Tests
{
    [TestFixture]
    public class InMemoryCacheStoreTests
    {
        private const string DummyUrl = "http://myserver/api/dummy";

        [Test]
        public void CanStore()
        {
            var store = new InMemoryCacheStore();
            store.AddOrUpdateAsync(new CacheKey(DummyUrl, new string[0]),
                ResponseHelper.GetOkMessage()).Wait();

            var r = store.GetValueAsync(new CacheKey(DummyUrl, new string[0])).Result;
            Assert.NotNull(r);
        }
    }
}
