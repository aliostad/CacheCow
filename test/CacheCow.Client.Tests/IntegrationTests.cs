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
using Xunit;

namespace CacheCow.Client.Tests
{
	
	public class IntegrationTests
	{
		[Fact(Skip = "Run manually")]
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
			Assert.NotNull(cacheCowHeader);
			Assert.Equal(true, cacheCowHeader.RetrievedFromCache);
		}
 	}
}
