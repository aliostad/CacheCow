#if NET462
using System.Net.Http;
#else
using Microsoft.AspNetCore.Http;
#endif
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server
{
    /// <summary>
    /// Decides whether response to an HTTP request can be cached
    /// </summary>
    public interface ICacheabilityValidator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">request</param>
        /// <returns>Whether response for this request is Cachebale</returns>
#if NET462
        bool IsCacheable(HttpRequestMessage request);
#else
        bool IsCacheable(HttpRequest request);
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response">request</param>
        /// <returns>Whether response for a request is Cachebale</returns>
#if NET462
        bool IsCacheable(HttpResponseMessage response);
#else
        bool IsCacheable(HttpResponse response);
#endif        

    }
}
