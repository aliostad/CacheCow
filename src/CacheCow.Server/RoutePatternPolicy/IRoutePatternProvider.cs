using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace CacheCow.Server.RoutePatternPolicy
{
    /// <summary>
    /// Responsible for providing route pattern and linked route patterns
    /// </summary>
    public interface IRoutePatternProvider
    {
        /// <summary>
        /// Gets route pattern for this request
        /// Mainly callled at the time of Key generation
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        string GetRoutePattern(HttpRequestMessage request);

        /// <summary>
        /// Gets all linked route patterns for this request.
        /// Called at the time of invalidation
        /// </summary>
        /// <param name="request">request</param>
        /// <returns>All linked route patterns for this request</returns>
        IEnumerable<string> GetLinkedRoutePatterns(HttpRequestMessage request);

    }
}
