using CacheCow.Samples.Common;
using CacheCow.Server.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CacheCow.Samples.MvcCore
{
    public class CarETagExtractor : ITimedETagExtractor<Car>
    {
        public TimedEntityTagHeaderValue Extract(Car viewModel)
        {
            if (viewModel == null)
                return null;
            return new TimedEntityTagHeaderValue(viewModel.LastModified.ToETagString());
        }

        public TimedEntityTagHeaderValue Extract(object viewModel)
        {
            return Extract(viewModel as Car);
        }
    }

    public class CarCollectionETagExtractor : ITimedETagExtractor<IEnumerable<Car>>
    {
        public TimedEntityTagHeaderValue Extract(IEnumerable<Car> viewModel)
        {
            if (viewModel == null)
                return null;

            return new TimedEntityTagHeaderValue(viewModel.GetMaxLastModified().ToETagString());
        }

        public TimedEntityTagHeaderValue Extract(object viewModel)
        {
            return Extract(viewModel as IEnumerable<Car>);
        }
    }

}
