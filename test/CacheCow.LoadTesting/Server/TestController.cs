using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using CacheCow.Server.CacheControlPolicy;
using CacheCow.Server.CacheRefreshPolicy;

namespace CacheCow.LoadTesting.Server
{
    public class TestController : ApiController
    {
        [HttpCacheRefreshPolicy(10)] 
        [HttpCacheControlPolicy(false, 5)] 
        public string GetBigString()
        {
            var bytes = new byte[256*1024];
            var random = new Random();
            random.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        [HttpCacheRefreshPolicy(10)]
        [HttpCacheControlPolicy(false, 5)]
        public string GetReverseString(string key)
        {
            return new string(key.Reverse().ToArray());
        }


    }
}
