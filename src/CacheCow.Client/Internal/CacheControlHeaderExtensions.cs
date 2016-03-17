using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Client.Internal
{
    internal static class CacheControlHeaderExtensions
    {
        public static bool ShouldRevalidate(this CacheControlHeaderValue headerValue, bool defaultBehaviour)
        {
            if (headerValue == null)
                return false;
            return defaultBehaviour || headerValue.MustRevalidate || headerValue.NoCache;
        }
    }
}
