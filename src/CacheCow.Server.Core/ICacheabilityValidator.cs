using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server.Core
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
        bool IsCacheable(HttpRequest request);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response">request</param>
        /// <returns>Whether response for a request is Cachebale</returns>
        bool IsCacheable(HttpResponse response);

    }
}
