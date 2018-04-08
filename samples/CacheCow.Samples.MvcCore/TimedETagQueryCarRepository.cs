using CacheCow.Samples.Common;
using CacheCow.Server.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheCow.Samples.MvcCore
{
    public class TimedETagQueryCarRepository : ITimedETagQueryProvider<IEnumerable<Car>>, ITimedETagQueryProvider<Car>
    {
        private readonly ICarRepository _repository;

        public TimedETagQueryCarRepository(ICarRepository repository)
        {
            _repository = repository;
        }

        public void Dispose()
        {
            // nothing
        }

        public Task<TimedEntityTagHeaderValue> QueryAsync(ResourceExecutingContext context)
        {
            int? id = 0;
            if (context.RouteData.Values.ContainsKey("id"))
                id = (int)context.RouteData.Values["id"];
            
            if(id.HasValue) // Get one car
            {
                var car = _repository.GetCar(id.Value);
                if (car != null)
                    return Task.FromResult(new TimedEntityTagHeaderValue(car.LastModified.ToETagString()));
                else
                    return Task.FromResult((TimedEntityTagHeaderValue)null);
            }
            else // all cars
            {               
                return Task.FromResult(new TimedEntityTagHeaderValue(_repository.GetMaxLastModified().ToETagString()));
            }
        }
    }

   
}
