using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server
{
    /// <summary>
    /// Caching Options
    /// </summary>
    public class HttpCachingOptions
    {
        /// <summary>
        /// Whether suppress emitting cachecow headers
        /// </summary>
        public bool DoNotEmitCacheCowHeader { get; set; }

        /// <summary>
        /// Whether look out for extracting expiry, etc values from the config
        /// </summary>
        public bool EnableConfiguration { get; set; } = false;
    }
}
