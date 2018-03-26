using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Common;
using System.Net.Http;
using System.Net;

namespace CacheCow.Client.RedisCacheStore.Tests
{
    [TestFixture]
    public class ExceptionPolicyTests
    {
        [Test]
        public void IfNotThrowThenDoesNot()
        {
            var s = new RedisStore("NoneExisting", throwExceptions: false);
            var k = new CacheKey("https://google.com/", new string[0]);
            var r = new HttpResponseMessage(HttpStatusCode.Accepted);
            s.AddOrUpdateAsync(k, r).Wait();
            var r2 = s.GetValueAsync(k).Result;
            var removed = s.TryRemoveAsync(k).Result;
            Assert.IsNull(r2, "response");
            Assert.IsFalse(removed, "removed");
        }
    }
}
