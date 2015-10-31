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
        [Ignore]
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
        [Ignore]
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
        [Explicit("This takes a long time to run.")]
		public void ExpiredClientCacheShallLoadFromServerAndUpdateExpiry()
        {
            using (var server = new InMemoryServer())
            using (var client = new HttpClient(new CachingHandler {InnerHandler = new HttpClientHandler()}))
            {
                string id = Guid.NewGuid().ToString();
				client.BaseAddress = new Uri(new Uri(TestConstants.BaseUrl), "/api/NoMustRevalidate/");
                server.Start();
                var response = client.GetAsync(id).Result;
                Assert.IsNull(response.Headers.GetCacheCowHeader().RetrievedFromCache);
                Assert.IsTrue(response.Headers.GetCacheCowHeader().DidNotExist.GetValueOrDefault());
                response = client.GetAsync(id).Result;
                Assert.IsTrue(response.Headers.GetCacheCowHeader().RetrievedFromCache.GetValueOrDefault());
				
				//TODO: Find a better way to make time pass. (:
				Thread.Sleep(TimeSpan.FromSeconds(5+1));

	            response = client.GetAsync(id).Result;
	            Assert.IsTrue(response.Headers.GetCacheCowHeader().RetrievedFromCache.GetValueOrDefault());
	            Assert.IsTrue(response.Headers.GetCacheCowHeader().WasStale.GetValueOrDefault());
	            Assert.LessOrEqual(DateTime.UtcNow - response.Headers.Date, TimeSpan.FromSeconds(1), "The cached item had expired and was refreshed, but the new retrieval date was not updated.");
			}
        }

        [Test]
        [Explicit("This takes a long time to run.")]
        public void ExpiredClientCacheShallLoadFromServerAndUpdateExpiryThenLoadFromCache()
        {
			using (var server = new InMemoryServer())
			using (var client = new HttpClient(new CachingHandler { InnerHandler = new HttpClientHandler() }))
			{
				string id = Guid.NewGuid().ToString();
				client.BaseAddress = new Uri(new Uri(TestConstants.BaseUrl), "/api/NoMustRevalidate/");
				server.Start();
				var response = client.GetAsync(id).Result;
				Assert.IsNull(response.Headers.GetCacheCowHeader().RetrievedFromCache);
				Assert.IsTrue(response.Headers.GetCacheCowHeader().DidNotExist.GetValueOrDefault());
				response = client.GetAsync(id).Result;
				Assert.IsTrue(response.Headers.GetCacheCowHeader().RetrievedFromCache.GetValueOrDefault());

				//TODO: Find a better way to make time pass. (:
				Thread.Sleep(TimeSpan.FromSeconds(5 + 1));

				response = client.GetAsync(id).Result;
				response = client.GetAsync(id).Result;
				Assert.IsTrue(response.Headers.GetCacheCowHeader().RetrievedFromCache.GetValueOrDefault());
				Assert.IsFalse(response.Headers.GetCacheCowHeader().WasStale.GetValueOrDefault(), "The cached item should have been refreshed but was instead considered stale.");
			}
		}

        [Test]
        [Ignore]
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
    }
}
