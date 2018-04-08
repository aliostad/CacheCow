using CacheCow.Samples.Common;
using CacheCow.Server.Core.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Samples.MvcCore
{
    [UseWebApiActionConventions]
    public class CarController : Controller
    {
        private readonly ICarRepository _repository;

        public CarController(ICarRepository repository)
        {
            this._repository = repository;
        }

        [HttpCacheFactory(0, ViewModelType = typeof(IEnumerable<Car>))]
        public IActionResult List()
        {
            return Ok(_repository.ListCars());
        }

        [HttpCacheFactory(0, ViewModelType = typeof(Car))]
        public IActionResult Get(int? id)
        {
            if (id.HasValue)
            {
                var car = _repository.GetCar(id.Value);
                return car == null
                    ? (IActionResult)new NotFoundResult()
                    : new ObjectResult(car);
            }
            else
                return List();
        }

        public IActionResult Delete(int id)
        {
            _repository.DeleteCar(id);
            return new NoContentResult();
        }

        public IActionResult PostCreateNew()
        {
            var car = _repository.CreateNewCar();
            return new CreatedResult($"/car/{car.Id}", car);
        }

        public IActionResult PutUpdateCar(int id)
        {
            var updated = _repository.UpdateCar(id);
            return updated
                ? (IActionResult) new OkResult()
                : new NotFoundResult();
        }
    }
}
