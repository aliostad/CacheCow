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
    public class CarsController : Controller
    {
        private readonly ICarRepository _repository;

        public CarsController(ICarRepository repository)
        {
            this._repository = repository;
        }

        [HttpGet]
        [HttpCacheFactory(0, ViewModelType = typeof(IEnumerable<Car>))]
        public IEnumerable<Car> List()
        {
            var cars = _repository.ListCars();
            return cars;
        }
    }
}
