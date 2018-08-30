using System.Net.Http;
using System.Threading.Tasks;
using CacheCow.Client.Headers;
using CacheCow.Common;
using Xunit;

namespace CacheCow.Client.Tests
{

    public class IntegrationTests
	{
		[Fact]
		public async Task Test_GoogleImage_WorksOnFirstSecondRequestNotThird()
		{
			const string Url = "https://ssl.gstatic.com/gb/images/j_e6a6aca6.png";
			var httpClient = new HttpClient(new CachingHandler()
												{
													InnerHandler = new HttpClientHandler()
												});
            httpClient.DefaultRequestHeaders.Add(HttpHeaderNames.Accept, "image/png");
			
			var httpResponseMessage = await httpClient.GetAsync(Url);
			var httpResponseMessage2 = await httpClient.GetAsync(Url);
			var cacheCowHeader = httpResponseMessage2.Headers.GetCacheCowHeader();
			Assert.NotNull(cacheCowHeader);
			Assert.Equal(true, cacheCowHeader.RetrievedFromCache);
		}
 	}
}
