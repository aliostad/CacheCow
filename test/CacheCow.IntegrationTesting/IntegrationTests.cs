using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CacheCow.Client;
using CacheCow.Client.Headers;
using CacheCow.IntegrationTesting.Server;
using NUnit.Framework;

namespace CacheCow.IntegrationTesting
{

    // NOTE!! Run with Administrative rights

    [TestFixture]
    public class IntegrationTests
    {
        [Test]
        public void NotModifiedReturnedAfter5Seconds()
        {
            // arrange
            using (var server = new InMemoryServer())
            using (var client = new HttpClient(new CachingHandler()
                                                   {
                                                       InnerHandler = new HttpClientHandler()
                                                   }))
            {
                string id = Guid.NewGuid().ToString();
                client.BaseAddress = new Uri(new Uri(TestConstants.BaseUrl), "/api/test/");
                server.Start();
                var response = client.GetAsync(id).Result;
                Assert.AreEqual(null, response.Headers.GetCacheCowHeader().RetrievedFromCache);
                Thread.Sleep(5000);
                response = client.GetAsync(id).Result;
                Assert.AreEqual(true, response.Headers.GetCacheCowHeader().RetrievedFromCache);
                Assert.AreEqual(true, response.Headers.GetCacheCowHeader().CacheValidationApplied);

            }
        }

        [Test]
        public void SecondRequestLoadsFromCache()
        {
            // arrange
            using (var server = new InMemoryServer())
            using (var client = new HttpClient(new CachingHandler()
            {
                InnerHandler = new HttpClientHandler()
            }))
            {
                string id = Guid.NewGuid().ToString();
                client.BaseAddress = new Uri(new Uri(TestConstants.BaseUrl), "/api/test/");
                server.Start();
                var response = client.GetAsync(id).Result;
                Assert.AreEqual(null, response.Headers.GetCacheCowHeader().RetrievedFromCache);
                Assert.AreEqual(true, response.Headers.GetCacheCowHeader().DidNotExist);
                response = client.GetAsync(id).Result;
                Assert.AreEqual(true, response.Headers.GetCacheCowHeader().RetrievedFromCache);
            }
        }
    }
}
