using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;

namespace CacheCow.Server.CachePolicy
{
    public interface ICachePolicy
    {
        CacheControlHeaderValue GetCacheControl(HttpRequestMessage request, HttpConfiguration configuration);
    }
}
