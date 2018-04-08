using CacheCow.Samples.Common;
using CacheCow.Server.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CacheCow.Samples.MvcCore
{
    public interface ITimedETagQueryCarRepository: ICarRepository, ITimedETagQueryProvider<IEnumerable<Car>>, ITimedETagQueryProvider<Car>
    {
    }
}
