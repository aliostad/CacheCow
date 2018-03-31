using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Common
{
    /// <summary>
    /// To be implemented by Models/ViewModels that can take part in HTTP Cache/Concurrency 
    /// </summary>
    public interface ICacheResource
    {
        /// <summary>
        /// Calculates/Returns a TimedETag which has either LastModified or ETag
        /// </summary>
        /// <returns>TimedETag</returns>
        TimedEntityTagHeaderValue GetTimedETag();
    }
}
