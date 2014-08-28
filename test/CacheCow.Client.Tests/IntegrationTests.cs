using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using CacheCow.Client;
using CacheCow.Client.Headers;
using NUnit.Framework;

namespace CacheCow.Client.Tests
{
	[TestFixture]
	public class IntegrationTests
	{
		[Test]
		[Ignore]
		public void Test_GoogleImage_WorksOnFirstSecondRequestNotThird()
		{
			const string Url = "https://ssl.gstatic.com/gb/images/j_e6a6aca6.png";
			var httpClient = new HttpClient(new CachingHandler()
												{
													InnerHandler = new HttpClientHandler()
												});
            httpClient.DefaultRequestHeaders.Add("Accept", "image/png");
			
			var httpResponseMessage = httpClient.GetAsync(Url).Result;
			var httpResponseMessage2 = httpClient.GetAsync(Url).Result;
			var cacheCowHeader = httpResponseMessage2.Headers.GetCacheCowHeader();
			Assert.IsNotNull(cacheCowHeader);
			Assert.AreEqual(true, cacheCowHeader.RetrievedFromCache);
		}

		[Test]
		[Ignore]
		public void Test_CarManager()
		{
			
			const string Url = "http://carmanager.azurewebsites.net/api/Car";
			var httpClient = new HttpClient(new CachingHandler()
			{
				InnerHandler = new HttpClientHandler()
			});


			var httpResponseMessage = httpClient.GetAsync(Url).Result;
			var httpResponseMessage2 = httpClient.GetAsync(Url).Result;
			var cacheCowHeader = httpResponseMessage2.Headers.GetCacheCowHeader();
			Assert.IsNotNull(cacheCowHeader);
			Assert.AreEqual(true, cacheCowHeader.RetrievedFromCache);
		}


        [Test]
        public void TestMemoryLeak()
        {
            var memorySize64 = Process.GetCurrentProcess().PrivateMemorySize64;
            for (int i = 0; i < 200; i++)
            {
                var store = new CachingHandler();
                //Thread.Sleep(1);
                store.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if(Process.GetCurrentProcess().PrivateMemorySize64 - memorySize64 > 2 * 1024 * 1024)
                    Assert.Fail("Memory leak");
            }
        }


	}
}
