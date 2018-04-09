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

        [HttpGet]
        [HttpCacheFactory(0, ViewModelType = typeof(IEnumerable<Car>))]
        public Car[] GetAll()
        {
            var cars = _repository.ListCars();
            return cars;
        }

        [HttpGet]
        [HttpCacheFactory(0, ViewModelType = typeof(Car))]
        public IActionResult Get(int id)
        {
            var car = _repository.GetCar(id);
            return car == null
                ? (IActionResult)new NotFoundResult()
                : new ObjectResult(car);
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            _repository.DeleteCar(id);
            return new NoContentResult();
        }

        [HttpPost]
        public IActionResult PostCreateNew()
        {
            var car = _repository.CreateNewCar();
            return new CreatedResult($"/car/{car.Id}", car);
        }

        [HttpPut]
        public IActionResult PutUpdateCar(int id)
        {
            var updated = _repository.UpdateCar(id);
            return updated
                ? (IActionResult) new OkResult()
                : new NotFoundResult();
        }
    }
}
