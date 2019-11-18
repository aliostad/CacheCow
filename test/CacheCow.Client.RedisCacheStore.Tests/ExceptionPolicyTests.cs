using System.Net;
using System.Net.Http;
using CacheCow.Common;
using Xunit;

namespace CacheCow.Client.RedisCacheStore.Tests
{
    public class ExceptionPolicyTests
    {
        [Fact]
        public void IfNotThrowThenDoesNot()
        {
            var s = new RedisStore("NoneExisting", throwExceptions: false);
            var k = new CacheKey("https://google.com/", new string[0]);
            var r = new HttpResponseMessage(HttpStatusCode.Accepted);
            s.AddOrUpdateAsync(k, r).Wait();
            var r2 = s.GetValueAsync(k).Result;
            var removed = s.TryRemoveAsync(k).Result;
            Assert.Null(r2);
            Assert.False(removed);
        }
    }
}
