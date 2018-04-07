using System;
using System.Collections.Generic;
using System.Linq;

namespace CacheCow.Samples.Common
{
    public class InMemoryCarRepository : ICarRepository
    {
        private Dictionary<int, Car> _cars = new Dictionary<int, Car>();
        private Random _random = new Random();

        public Car CreateNewCar()
        {
            var car = new Car()
            {
                Id = _cars.Count == 0 ? 1 : _cars.Values.Max(x => x.Id) + 1,
                LastModified = DateTimeOffset.Now,
                NumberPlate = new string(Enumerable.Range(0, 3).Select(x => Convert.ToChar(((int)'A') + _random.Next(0, 26)))
                    .Concat(Enumerable.Range(0, 4).Select(x => _random.Next(10).ToString()[0])).ToArray()),
                Year = _random.Next(2000, 2018)
            };

            return car;
        }

        public bool DeleteCar(int id)
        {
            if (_cars.ContainsKey(id))
            {
                _cars.Remove(id);
                return true;
            }
            else
                return false;
        }

        public Car GetCar(int id)
        {
            return _cars.ContainsKey(id) ? _cars[id] : null;
        }

        public IEnumerable<Car> ListCars()
        {
            return _cars.Values.OrderBy(x => x.Id);
        }

        public bool UpdateCar(int id)
        {
            if (_cars.ContainsKey(id))
            {
                _cars[id].LastModified = DateTimeOffset.Now;
                return true;
            }
            else
                return false;
        }
    }
}
