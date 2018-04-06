using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Server.Core
{
    /// <summary>
    /// Main interface for providing Cache headers for a resource. 
    /// Use generic interface if you can.
    /// </summary>
    public interface ICacheDirectiveProvider : ITimedETagExtractor
    {
        CacheControlHeaderValue Get(HttpContext context);
    }

    /// <summary>
    /// Main interface for providing Cache headers for a resource.
    /// </summary>
    public interface ICacheDirectiveProvider<TViewModel> : ICacheDirectiveProvider
    {
        CacheControlHeaderValue Get(HttpContext context);
    }
}
