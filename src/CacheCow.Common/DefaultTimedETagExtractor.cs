using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Common
{
    /// <summary>
    /// Default impl where it tries to cast as ICacheResource and if successful, calls the method
    /// </summary>
    public class DefaultTimedETagExtractor : ITimedETagExtractor
    {
        public TimedEntityTagHeaderValue Extract(object viewModel)
        {
            var resource = viewModel as ICacheResource;
            if (resource == null)
                return null;
            return resource.GetTimedETag();
        }
    }

    public class DefaultTimedETagExtractor<TViewModel> : DefaultTimedETagExtractor, ITimedETagExtractor<TViewModel>
    {
        public TimedEntityTagHeaderValue Extract(TViewModel t)
        {
            return base.Extract(t);
        }
    }

}
