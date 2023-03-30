using System.Net.Http;
using System.Threading.Tasks;
using CacheCow.Client.Headers;
using CacheCow.Common;
using Xunit;
using System;
using System.IO;
using System.Net;

namespace CacheCow.Client.Tests
{

    public class IntegrationTests
    {
        public const string Url = "https://ssl.gstatic.com/gb/images/j_e6a6aca6.png";

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
        public async Task Simple_Caching_Example_From_Issue263()
        {
#if NET462
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#endif

            var client = ClientExtensions.CreateClient();
            const string CacheableResource = "https://code.jquery.com/jquery-3.3.1.slim.min.js";
            var response = await client.GetAsync(CacheableResource);
            var responseFromCache = await client.GetAsync(CacheableResource);
            Assert.Equal(true, response.Headers.GetCacheCowHeader().DidNotExist);
            Assert.Equal(true, responseFromCache.Headers.GetCacheCowHeader()?.RetrievedFromCache);
        }


        [Fact] // Skip if the resource becomes unavailable
        public async Task Simple_Caching_Example_From_Issue267()
        {
#if NET462
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#endif

            var client = ClientExtensions.CreateClient();

            // this one does not have a content-type too and that could be somehow related
            // but not quite sure. Have used other places where a chunked encoding might
            // be returned but did not cause the same problem
            const string CacheableResource = "https://webhooks.truelayer-sandbox.com/.well-known/jwks";

            var response = await client.GetAsync(CacheableResource);
            var body = await response.Content.ReadAsByteArrayAsync();
            var responseFromCache = await client.GetAsync(CacheableResource);
            if (responseFromCache.Content == null)
            {
                throw new InvalidOperationException("Response content from cache is null");
            }

            var bodyFromCache = await responseFromCache.Content.ReadAsByteArrayAsync();
            Assert.Equal(true, response.Headers.GetCacheCowHeader()?.DidNotExist);
            Assert.Equal(body.Length, bodyFromCache.Length);
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
