using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Common;
#if NET452
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

#if NET452
        public Task<TimedEntityTagHeaderValue> QueryAsync(HttpActionContext context)
#else
        public Task<TimedEntityTagHeaderValue> QueryAsync(ResourceExecutingContext context)
#endif
        {
            return Task.FromResult((TimedEntityTagHeaderValue)null);
        }
    }

#if NET452
#else
    public class NullQueryProvider<T> : NullQueryProvider, ITimedETagQueryProvider<T>
    {

    }
#endif
}