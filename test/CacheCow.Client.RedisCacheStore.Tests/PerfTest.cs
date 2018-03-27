using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Common;
using System.Net.Http;
using System.Net;
using System.Diagnostics;

namespace CacheCow.Client.RedisCacheStore.Tests
{
    
    public class PerfTest
    {
        private const string ConnectionString = "localhost";
        private const string UrlStem = "http://foofoo.com/";
        private Random _r = new Random();

        [Fact(Skip = "Requires local redis running")]
        public void AccessingLocalRedis100TimesLessThanASecond()
        {
            var c = new RedisStore(ConnectionString);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                var buffer = new byte[_r.Next(4 * 1024, 64 * 1024)];
                _r.NextBytes(buffer);
                var key = new CacheKey(UrlStem + Guid.NewGuid().ToString(), new string[0]);
                var task = c.AddOrUpdateAsync(key,
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(buffer)
                    });
                task.GetAwaiter().GetResult();
                var r = c.GetValueAsync(key).GetAwaiter().GetResult();
            }

            if(sw.ElapsedMilliseconds > 1000)
            {
                throw new Exception("Took more than 1000 ms");
            }
        }
    }
}
