using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server.Core.Mvc.Tests
{
    public class BetterTestController : Controller
    {
        [HttpCacheFactory(expirySeconds: 10)]
        public TestViewModel Get(int id)
        {
            return new ResourceTestViewModel()
            {
                Id = id,
                Name = "JLH",
                LastModified = new DateTimeOffset(2018, 04, 01, 0, 0, 0, TimeSpan.FromHours(1))
            };
        }

        [HttpCacheFactory(expirySeconds: 0)]
        public IEnumerable<TestViewModel> GetAll()
        {
            return new []{
                new ResourceTestViewModel()
                {
                    Id = 1,
                    Name = "JLH",
                    LastModified = new DateTimeOffset(2018, 04, 01, 0, 0, 0, TimeSpan.FromHours(1))
                },
                new ResourceTestViewModel()
                {
                    Id = 12,
                    Name = "RJ",
                    LastModified = new DateTimeOffset(2018, 04, 02, 0, 0, 0, TimeSpan.FromHours(1))
                }
            };
        }

    }
}
