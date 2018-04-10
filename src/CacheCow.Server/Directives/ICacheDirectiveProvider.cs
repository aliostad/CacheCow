#if NET452
using System.Web.Http.Filters;
#else
using Microsoft.AspNetCore.Http;
#endif
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Server
{
    /// <summary>
    /// Main interface for providing Cache headers for a resource. 
    /// Use generic interface if you can.
    /// </summary>
    public interface ICacheDirectiveProvider : ITimedETagExtractor, ITimedETagQueryProvider
    {
#if NET452
        CacheControlHeaderValue GetCacheControl(HttpActionExecutedContext context, TimeSpan? configuredExpiry);
        IEnumerable<string> GetVaryHeaders(HttpActionExecutedContext context);
#else
        CacheControlHeaderValue GetCacheControl(HttpContext context, TimeSpan? configuredExpiry);
        IEnumerable<string> GetVaryHeaders(HttpContext context);
#endif
    }

#if NET452
#else
    /// <summary>
    /// Main interface for providing Cache headers for a resource.
    /// </summary>
    public interface ICacheDirectiveProvider<TViewModel> : ICacheDirectiveProvider, ITimedETagQueryProvider<TViewModel>
    {
    }

#endif
}
