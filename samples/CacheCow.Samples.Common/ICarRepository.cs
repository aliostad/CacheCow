using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Samples.Common
{
    public interface ICarRepository
    {
        Car CreateNewCar();

        bool UpdateCar(int id);

        bool DeleteCar(int id);

        Car GetCar(int id);

        IEnumerable<Car> ListCars();

        DateTimeOffset GetMaxLastModified();
    }
}
