using CacheCow.Common;
#if NET462
using System.Web.Http.Controllers;
#else
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CacheCow.Server
{
    /// <summary>
    /// Provides TETHV by querying a store to get the most recent value
    /// It is advisable to use the generic interface instead of this one otherwise impl of this interface becomes catch-all for all viewmodel types.
    /// </summary>
    public interface  ITimedETagQueryProvider : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// 
#if NET462
        Task<TimedEntityTagHeaderValue> QueryAsync(HttpActionContext context);
#else
        Task<TimedEntityTagHeaderValue> QueryAsync(HttpContext context);
#endif
    }

    /// <summary>
    /// Provides TETHV by querying a store to get the most recent value
    /// This is the cornerstore construct for Cache Validation and conditional GET or PUT. By implementing this interface for your resource, you reduce load on your backend systems.
    /// </summary>
    /// <typeparam name="T">This type is mainly used for ease of dependency injection</typeparam>
    public interface ITimedETagQueryProvider<T> : ITimedETagQueryProvider
    {        
    }

}
