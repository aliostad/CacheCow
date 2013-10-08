using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CacheCow.IntegrationTesting.Server;
using NUnit.Framework;

namespace CacheCow.IntegrationTesting
{
    [TestFixture]
    public class IntegrationTests
    {
        [Test]
        public void NotModifiedReturnedAfter5Seconds()
        {
            // arrange
            using (var server = new InMemoryServer())
            using (var client = new HttpClient())
            {
                string id = Guid.NewGuid().ToString();
                client.BaseAddress = new Uri(new Uri(TestConstants.BaseUrl), "api/test");
                server.Start();
                var response = client.GetAsync(id).Result;

            }
        }
    }
}
