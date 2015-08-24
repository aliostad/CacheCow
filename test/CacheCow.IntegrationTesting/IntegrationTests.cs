using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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

        [Test]
        public void ZeroMaxAgeShouldAlwaysComeFromCacheIfNotChanged()
        {
            // arrange
            using (var server = new InMemoryServer())
            using (var client = new HttpClient(new CachingHandler()
            {
                InnerHandler = new HttpClientHandler()
            }))
            {
                string id = Guid.NewGuid().ToString();
                client.BaseAddress = new Uri(new Uri(TestConstants.BaseUrl), "/api/ZeroMaxAge/");
                server.Start();

                Trace.WriteLine("STARTING FIRST _______________________________________________________________________________");

                var response = client.GetAsync(id).Result;
                var header = response.Headers.GetCacheCowHeader();
                Trace.WriteLine("CacheCowHeader=> " + header);
                Assert.AreEqual(null, header.RetrievedFromCache, "First RetrievedFromCache");
                Assert.AreEqual(true, header.DidNotExist, "First DidNotExist");

                Thread.Sleep(2000);


                // second time
                Trace.WriteLine("STARTING SECOND _______________________________________________________________________________");
                response = client.GetAsync(id).Result;
                header = response.Headers.GetCacheCowHeader();
                Trace.WriteLine("CacheCowHeader=> " + header);
                Assert.AreEqual(true, header.RetrievedFromCache, "Second RetrievedFromCache");
                Assert.AreEqual(true, header.WasStale, "Second WasStale");

                Thread.Sleep(1000);

                // third time
                Trace.WriteLine("STARTING THIRD _______________________________________________________________________________");
                response = client.GetAsync(id).Result;
                header = response.Headers.GetCacheCowHeader();
                Trace.WriteLine("CacheCowHeader=> " + header);
                Assert.AreEqual(true, header.RetrievedFromCache, "Third RetrievedFromCache");
                Assert.AreEqual(true, header.WasStale, "Third WasStale");


            }          
        }

        [Test]
        public void NotModifiedReturnsCorrectCacheControlHeaders()
        {
            using (var server = new InMemoryServer())
            {
                server.Start();
                var client = new HttpClient();
                string id = Guid.NewGuid().ToString();
                client.BaseAddress = new Uri(new Uri(TestConstants.BaseUrl), "/api/ZeroMaxAge/");
                var response1 = client.GetAsync(id).Result;
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, TestConstants.BaseUrl + "/api/ZeroMaxAge/" + id);
                requestMessage.Headers.Add("If-Modified-Since", response1.Headers.Date.Value.ToString("r"));
                var response2 = client.SendAsync(requestMessage).Result;
                Assert.AreEqual(response2.StatusCode, HttpStatusCode.NotModified);
                Assert.AreEqual(response1.Headers.CacheControl.NoCache, response2.Headers.CacheControl.NoCache);
                Assert.AreEqual(response1.Headers.CacheControl.MaxAge, response2.Headers.CacheControl.MaxAge);
            }
        }
    }
}
