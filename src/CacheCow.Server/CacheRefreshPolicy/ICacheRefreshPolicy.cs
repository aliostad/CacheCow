using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;

namespace CacheCow.Server.CacheRefreshPolicy
{
    public interface ICacheRefreshPolicy
    {
        TimeSpan GetCacheControl(HttpRequestMessage request, HttpConfiguration configuration);
    }
}
