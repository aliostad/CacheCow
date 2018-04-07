using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CacheCow.Server.Core
{
    public abstract class CacheDirectiveProviderBase : ICacheDirectiveProvider
    {
        private readonly ITimedETagExtractor _timedETagExtractor;
        private readonly ITimedETagQueryProvider _queryProvider;

        public CacheDirectiveProviderBase(ITimedETagExtractor timedETagExtractor, ITimedETagQueryProvider queryProvider)
        {
            _timedETagExtractor = timedETagExtractor;
            _queryProvider = queryProvider;
        }

        public TimedEntityTagHeaderValue Extract(object viewModel)
        {
            return _timedETagExtractor.Extract(viewModel);
        }

        public abstract CacheControlHeaderValue GetCacheControl(HttpContext context, TimeSpan? configuredExpiry);

        public Task<TimedEntityTagHeaderValue> QueryAsync(ResourceExecutingContext context)
        {
            return _queryProvider.QueryAsync(context);
        }

        public virtual void Dispose()
        {
            _queryProvider.Dispose();            
        }

        public IEnumerable<string> GetVaryHeaders(HttpContext context)
        {
            return new[] {"Accept"};
        }
    }
}
