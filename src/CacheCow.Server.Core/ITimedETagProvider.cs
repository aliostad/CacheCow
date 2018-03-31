using System;
using System.Collections.Generic;
using System.Text;
using CacheCow.Common;

namespace CacheCow.Server.Core
{
    /// <summary>
    /// Responsible for returning TimedEntityTagHeaderValue for a resource (Model/ViewModel) returned from the controller.
    /// This is the cornerstore construct for Cache Validation and conditional GET or PUT. By implementing this interface for your resource, you reduce load on your backend systems.
    /// </summary>
    /// <typeparam name="T">Normally a Model/ViewModel that is sent back from the controller</typeparam>
    public interface ITimedETagProvider<T> : IDisposable
     where T : class
    {
        /// <summary>
        /// Finds TimedEntityTagHeaderValue for a resource (model/viewmodel). 
        /// In the process, it could connect to database/store to find the LastModified or ETag.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        TimedEntityTagHeaderValue Get(T t);
    }
}
