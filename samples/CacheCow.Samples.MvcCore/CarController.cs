using CacheCow.Samples.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Samples.MvcCore
{
    public class CarController
    {
        private readonly ICarRepository _repository;

        public CarController(ICarRepository repository)
        {
            this._repository = repository;
        }

        public IEnumerable<Car> GetAll()
        {
            return _repository.ListCars();
        }

        public IActionResult Get(int id)
        {
            var car = _repository.GetCar(id);
            return car == null
                ? (IActionResult) new NotFoundResult()
                : new ObjectResult(car);
        }
    }
}
