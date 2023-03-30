using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Common;
#if NET462
using System.Web.Http.Controllers;
#else
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
#endif


namespace CacheCow.Server
{
    /// <summary>
    /// Essentially returns null and does not implement querying. It is the polyfill and default.
    /// </summary>
    public class NullQueryProvider : ITimedETagQueryProvider
    {
        public void Dispose()
        {
        }

#if NET462
        public Task<TimedEntityTagHeaderValue> QueryAsync(HttpActionContext context)
#else
        public Task<TimedEntityTagHeaderValue> QueryAsync(HttpContext context)
#endif
        {
            return Task.FromResult((TimedEntityTagHeaderValue)null);
        }
    }

#if NET462
#else
    public class NullQueryProvider<T> : NullQueryProvider, ITimedETagQueryProvider<T>
    {

    }
#endif
}