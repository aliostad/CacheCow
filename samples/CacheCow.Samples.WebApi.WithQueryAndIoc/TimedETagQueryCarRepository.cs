using CacheCow.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using CacheCow.Samples.Common;

namespace CacheCow.Samples.WebApi.WithQueryAndIoc
{
    public class TimedETagQueryCarRepository : ITimedETagQueryProvider, ITimedETagQueryProvider<Car>, ITimedETagQueryProvider<IEnumerable<Car>>
    {
        private readonly ICarRepository _repository;

        public TimedETagQueryCarRepository(ICarRepository repository)
        {
            this._repository = repository;
        }

        public void Dispose()
        {
            // none
        }

        public Task<TimedEntityTagHeaderValue> QueryAsync(HttpActionContext context)
        {
            int? id = null;
            if (context.RequestContext.RouteData.Values.ContainsKey("id"))
                id = Convert.ToInt32(context.RequestContext.RouteData.Values["id"]);

            if (id.HasValue) // Get one car
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
