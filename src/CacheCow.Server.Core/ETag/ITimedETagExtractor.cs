using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server.Core
{
    /// <summary>
    /// Non-generic interface. Meant for internal use - please use the generic interface.
    /// </summary>
    public interface ITimedETagExtractor
    {
        TimedEntityTagHeaderValue Extract(object viewModel);
    }
}
