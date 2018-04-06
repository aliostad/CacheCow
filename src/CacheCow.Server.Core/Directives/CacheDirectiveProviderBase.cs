using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace CacheCow.Server.Core
{
    public abstract class CacheDirectiveProviderBase : ICacheDirectiveProvider
    {
        private readonly ITimedETagExtractor _timedETagExtractor;

        public CacheDirectiveProviderBase(ITimedETagExtractor timedETagExtractor)
        {
            _timedETagExtractor = timedETagExtractor;
        }

        /// <summary>
        /// Whether it is worth trying to extracting TETHV
        /// If resource is non-cacheable it is not worth it
        /// </summary>
        /// <returns></returns>
        protected abstract bool ShouldTryExtract();

        public TimedEntityTagHeaderValue Extract(object viewModel)
        {
            return ShouldTryExtract() ? _timedETagExtractor.Extract(viewModel) : null;
        }

        public abstract CacheControlHeaderValue Get(HttpContext context);
        
    }

    public abstract class CacheDirectiveProviderBase<TViewModel> : CacheDirectiveProviderBase, ICacheDirectiveProvider<TViewModel>
    {
        public CacheDirectiveProviderBase(ITimedETagExtractor timedETagExtractor) : base(timedETagExtractor)
        {
        }

        public TimedEntityTagHeaderValue Extract(TViewModel t)
        {
            return base.Extract(t);
        }
    }

}
