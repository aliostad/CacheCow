using CacheCow.Samples.Common;
using CacheCow.Server;
using Carter;
using Carter.Request;
using Carter.Response;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Samples.Carter
{
    public class CarModule : CarterModule
    {
        public CarModule(ICarRepository carRepo,
            ICachingPipeline<IEnumerable<Car>> carsPipeline,
            ICachingPipeline<Car> carPipeline)
        {

            carsPipeline.ConfiguredExpiry = TimeSpan.Zero; // for strong consistency
            carPipeline.ConfiguredExpiry = TimeSpan.Zero; // for strong consistency

            // get all cars
            Get("/api/cars", async (context) =>
            {
                var carryOn = await carsPipeline.Before(context);
                if (!carryOn)
                    return;

                var viewModel = carRepo.ListCars();
                await context.Response.Negotiate(viewModel);
                carsPipeline.After(context, viewModel);
            });

            // get a car
            Get("/api/car/{id:int}", async (context) =>
            {
                var id = context.GetRouteData().As<int>("id");
                var carryOn = await carPipeline.Before(context);
                if (!carryOn)
                    return;

                var viewModel = carRepo.GetCar(id);
                await context.Response.Negotiate(viewModel);
                carPipeline.After(context, viewModel);
            });

            // create a car
            Post("/api/car", async (context) =>
            {
                var carryOn = await carPipeline.Before(context);
                if (!carryOn)
                    return;

                var car = carRepo.CreateNewCar();

                context.Response.StatusCode = 201;
                context.Response.Headers.Add("Location", "/api/car/" + car.Id);
                carPipeline.After(context, car);
            });

            // update a car
            Put("/api/car/{id:int}", async (context) =>
            {
                var id = context.GetRouteData().As<int>("id");
                var carryOn = await carPipeline.Before(context);
                if (!carryOn)
                    return;

                var updated = carRepo.UpdateCar(id);

                if(updated)
                    context.Response.StatusCode = 200;
                else
                    context.Response.StatusCode = 404;

                carPipeline.After(context, null);
            });

            // delete a car
            Delete("/api/car/{id:int}", async (context) =>
            {
                var id = context.GetRouteData().As<int>("id");
                var carryOn = await carPipeline.Before(context);
                if (!carryOn)
                    return;

                var updated = carRepo.DeleteCar(id);

                if (updated)
                    context.Response.StatusCode = 200;
                else
                    context.Response.StatusCode = 404;

                carPipeline.After(context, null);
            });


        }
    }
}
