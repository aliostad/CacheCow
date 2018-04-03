using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Common;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CacheCow.Server.Core
{
    /// <summary>
    /// Essentially returns null and does not implement querying. It is the polyfill and default.
    /// </summary>
    public class NullQueryProvider : ITimedETagQueryProvider
    {
        public void Dispose()
        {
        }

        public Task<TimedEntityTagHeaderValue> QueryAsync(ResourceExecutingContext context)
        {
            return Task.FromResult((TimedEntityTagHeaderValue)null);
        }
    }

    public class NullQueryProvider<T> : NullQueryProvider, ITimedETagQueryProvider<T>
    {

    }
}