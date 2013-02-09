using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Common;
using NUnit.Framework;

namespace CacheCow.Client.MemcachedCacheStore.Tests
{
    [TestFixture]
    public class IntegrationTests
    {

        private HttpResponseMessage _response;
        private CacheKey _key;
        public IntegrationTests()
        {
            _response = new HttpClient().GetAsync("http://google.com").Result;
            _key = new CacheKey(_response.RequestMessage.RequestUri.AbsoluteUri, new string[0]);
        }

        [Ignore]
        [Test]
        public void TestGet()
        {
            using (var mc = new MemcachedStore())
            {
                mc.AddOrUpdate(_key, _response);

                HttpResponseMessage message;

                var success = mc.TryGetValue(_key, out message);
                Assert.AreEqual(_response.Content.Headers.ContentLength, message.Content.Headers.ContentLength);
                Assert.AreEqual(_response.Content.ReadAsStringAsync().Result, 
                    message.Content.ReadAsStringAsync().Result);
                Assert.IsTrue(success);
            }
        }

        [Ignore]
        [Test]
        public void TestRemove()
        {
            using (var mc = new MemcachedStore())
            {
                mc.AddOrUpdate(_key, _response);
                HttpResponseMessage message;


                var success = mc.TryRemove(_key);
                var getFailure = mc.TryGetValue(_key, out message);

                var failure = mc.TryRemove(_key);

                Assert.IsTrue(success);
                Assert.IsTrue(!failure);
                Assert.IsTrue(!getFailure);
            }
            
        }

    }
}
