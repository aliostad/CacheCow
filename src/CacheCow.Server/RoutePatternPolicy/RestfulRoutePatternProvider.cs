using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace CacheCow.Server.RoutePatternPolicy
{
    /// <summary>
    /// Assumes you use GUIDs as identifiers. If you use ints (or something else entirely), just tweak the parsing
    /// </summary>
    public class RestfulRoutePatternProvider : IRoutePatternProvider
    {
        public string GetRoutePattern(HttpRequestMessage request)
        {
            var uri = request.RequestUri.AbsolutePath;
            return uri[0] == '/' ? uri.Substring(1) : uri;
        }

        public IEnumerable<string> GetLinkedRoutePatterns(HttpRequestMessage request)
        {
            var uri = request.RequestUri.AbsolutePath;
            var segments = uri.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToList();
            Guid guid;
            var patterns = new List<string>();
            if (Guid.TryParse(segments.Last(), out guid))
            {
                if (segments.Count > 2)
                {
                    patterns.Add(string.Join("/", segments.GetRange(0, segments.Count() - 1))); // everything but the Guid at the end
                }
                if (segments.Count > 3)
                {
                    var directGetterSegments = new List<string> { segments.First() };
                    directGetterSegments.AddRange(segments.GetRange(segments.Count() - 2, 2));
                    patterns.Add(string.Join("/", directGetterSegments)); // handles direct getters of the entity (e.g. api/stuff/Guid)
                    patterns.Add(string.Join("/", directGetterSegments.GetRange(0, directGetterSegments.Count - 1))); // handle direct collection getters of the entity (e.g. api/stuff)
                }
            }
            return patterns;
        }
    }
}
