using System.Net.Http;
using System.Threading.Tasks;
using CacheCow.Client.Headers;
using CacheCow.Common;
using Xunit;

namespace CacheCow.Client.Tests
{

    public class IntegrationTests
	{
        const string Url = "https://ssl.gstatic.com/gb/images/j_e6a6aca6.png";

		[Fact]
		public async Task Test_GoogleImage_WorksOnFirstSecondRequestNotThird()
		{
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

        [Fact]
        public async Task SettingNoHeaderWorks()
        {
            var cachecow = new CachingHandler()
            {
                DoNotEmitCacheCowHeader = true,
                InnerHandler = new HttpClientHandler()
            };

            var client = new HttpClient(cachecow);

            var request1 = new HttpRequestMessage(HttpMethod.Get, Url);
            var request2 = new HttpRequestMessage(HttpMethod.Get, Url);

            var response = await client.SendAsync(request1);
            var responseFromCache = await client.SendAsync(request2);

            var h = responseFromCache.Headers.GetCacheCowHeader();

            Assert.Null(h);
        }
 	}
}
