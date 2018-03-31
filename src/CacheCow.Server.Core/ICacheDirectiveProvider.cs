using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Server.Core
{
    /// <summary>
    /// Main interface for providing Cache headers for a resource
    /// </summary>
    public interface ICacheDirectiveProvider : IDisposable
    {
        CacheControlHeaderValue Get(HttpContext context);
    }
}
