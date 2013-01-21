using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
		public void Test_GoogleImage()
		{
			const string Url = "https://ssl.gstatic.com/gb/images/j_e6a6aca6.png";
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



	}
}
