using CacheCow.Server;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CacheCow.Samples.Common
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

    public class CarAndCollectionETagExtractor : ITimedETagExtractor
    {
        public TimedEntityTagHeaderValue Extract(object viewModel)
        {
            var car = viewModel as Car;
            if(car != null)
                return new TimedEntityTagHeaderValue(car.LastModified.ToETagString());
            var cars = viewModel as IEnumerable<Car>;
            if (cars != null)
                return new TimedEntityTagHeaderValue(cars.GetMaxLastModified().ToETagString());

            return null;
        }
    }

}
