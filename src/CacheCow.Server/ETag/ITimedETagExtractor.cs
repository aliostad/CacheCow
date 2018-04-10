using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server
{
    /// <summary>
    /// Non-generic interface. Meant for internal use - please use the generic interface.
    /// </summary>
    public interface ITimedETagExtractor
    {
        TimedEntityTagHeaderValue Extract(object viewModel);
    }

    /// <summary>
    /// Generic version
    /// </summary>
    /// <typeparam name="TViewModel"></typeparam>
    public interface ITimedETagExtractor<TViewModel> : ITimedETagExtractor
    {
        TimedEntityTagHeaderValue Extract(TViewModel viewModel);
    }

}
