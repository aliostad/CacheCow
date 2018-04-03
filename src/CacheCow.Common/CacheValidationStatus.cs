using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Common
{
    /// <summary>
    /// Status of cache validation required for a request
    /// </summary>
    public enum CacheValidationStatus
    {
        None = 0,
        GetIfModifiedSince = 1,
        GetIfNoneMatch = 2,
        PutIfUnModifiedSince = 3,
        PutIfMatch = 4
    }
}
