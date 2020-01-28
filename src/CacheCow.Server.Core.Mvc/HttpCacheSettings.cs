using System;

namespace CacheCow.Server.Core.Mvc
{
    /// <summary>
    /// Settings to be read from configuration
    /// </summary>
    public class HttpCacheSettings
    {
        /// <summary>
        /// Whether ignore the filter or not
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Cache expiry
        /// </summary>
        public TimeSpan? Expiry { get; set; }

    }
}
