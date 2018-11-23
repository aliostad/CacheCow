﻿using CacheCow.Samples.Common;
using CacheCow.Server.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace CacheCow.Samples.WebApi.WithQueryAndIoc
{
    public class CarController : ApiController
    {
        private readonly ICarRepository _repository;

        public CarController(ICarRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [HttpCache(DefaultExpirySeconds = 0)]
        public IHttpActionResult Get(int id)
        {
            var car = _repository.GetCar(id);
            return car == null
                ? (IHttpActionResult) new NotFoundResult(this)
                : Ok(car);
        }

        [HttpGet]
        [HttpCache(DefaultExpirySeconds = 0)]
        public IEnumerable<Car> GetAll()
        {
            var cars = _repository.ListCars();
            return cars;
        }

        [HttpDelete]
        [HttpCache(ViewModelType = typeof(Car))]
        public IHttpActionResult Delete(int id)
        {
            _repository.DeleteCar(id);
            return new StatusCodeResult(HttpStatusCode.NoContent, this);
        }

        [HttpPost]
        public IHttpActionResult PostCreateNew()
        {
            var car = _repository.CreateNewCar();
            return new CreatedNegotiatedContentResult<Car>(new Uri($"/car/{car.Id}", uriKind: UriKind.Relative), car, this);
        }

        [HttpPut]
        [HttpCache]
        public IHttpActionResult PutUpdateCar(int id)
        {
            var updated = _repository.UpdateCar(id);
            return updated
                ? (IHttpActionResult) Ok()
                : new NotFoundResult(this);
        }
    }
}

