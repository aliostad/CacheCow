using CacheCow.Samples.Common;
using CacheCow.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CacheCow.Samples.Carter
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

        public Task<TimedEntityTagHeaderValue> QueryAsync(HttpContext context)
        {
            int? id = null;
            var routeData = context.GetRouteData();
            if (routeData.Values.ContainsKey("id"))
                id = Convert.ToInt32(routeData.Values["id"]);
            
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
                return Task.FromResult(new TimedEntityTagHeaderValue(_repository.GetMaxLastModified().ToETagString(_repository.GetCount())));
            }
        }
    }

   
}
