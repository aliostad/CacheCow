using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server.Core.Mvc.Tests.Integration
{
    public class WithQueryController
    {
        [HttpCacheFactory(expirySeconds: 0, ViewModelType = typeof(TestViewModel))]
        public TestViewModel Get(int id)
        {
            return new TestViewModel()
            {
                Id = id,
                Name = "JLH",
                LastModified = new DateTimeOffset(2018, 04, 01, 0, 0, 0, TimeSpan.FromHours(1))
            };
        }

        [HttpCacheFactory(expirySeconds: 0, ViewModelType = typeof(IEnumerable<TestViewModel>))]
        public IEnumerable<TestViewModel> GetAll()
        {
            return new[]{
                new TestViewModel()
                {
                    Id = 1,
                    Name = "JLH",
                    LastModified = new DateTimeOffset(2018, 04, 01, 0, 0, 0, TimeSpan.FromHours(1))
                },
                new TestViewModel()
                {
                    Id = 12,
                    Name = "RJ",
                    LastModified = new DateTimeOffset(2018, 04, 02, 0, 0, 0, TimeSpan.FromHours(1))
                }
            };
        }
    }
}
