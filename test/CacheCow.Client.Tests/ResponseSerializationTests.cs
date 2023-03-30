using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using CacheCow.Client;
using CacheCow.Common;
using Xunit;
using System.Threading.Tasks;

namespace CacheCow.Client.Tests
{

    public class ResponseSerializationTests
    {
        [Fact]
        public async Task IntegrationTest_Deserialize()
        {
            var httpClient = new HttpClient();
            var httpResponseMessage = await httpClient.GetAsync(IntegrationTests.Url);
            Console.WriteLine(httpResponseMessage.Headers.ToString());
            var defaultHttpResponseMessageSerializer = new MessageContentHttpMessageSerializer();
            var fileStream = new FileStream("msg.bin", FileMode.Create);
            await defaultHttpResponseMessageSerializer.SerializeAsync(httpResponseMessage, fileStream);
            fileStream.Close();

            var fileStream2 = new FileStream("msg.bin", FileMode.Open);
            var httpResponseMessage2 = await defaultHttpResponseMessageSerializer.DeserializeToResponseAsync(fileStream2);
            fileStream.Close();
        }

        [Fact]
        public async Task IntegrationTest_Serialize_Deserialize()
        {
#if NET462
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#endif

            var httpClient = new HttpClient();
            var httpResponseMessage = await httpClient.GetAsync("https://webhooks.truelayer-sandbox.com/.well-known/jwks");
            var memoryStream = new MemoryStream();

            var defaultHttpResponseMessageSerializer = new MessageContentHttpMessageSerializer();
            await defaultHttpResponseMessageSerializer.SerializeAsync(httpResponseMessage, memoryStream);

            memoryStream.Position = 0;
            var httpResponseMessage2 = await defaultHttpResponseMessageSerializer.DeserializeToResponseAsync(memoryStream);
            Assert.Equal(httpResponseMessage.StatusCode, httpResponseMessage2.StatusCode);
            Assert.Equal(httpResponseMessage.ReasonPhrase, httpResponseMessage2.ReasonPhrase);
            Assert.Equal(httpResponseMessage.Version, httpResponseMessage2.Version);
            Assert.Equal(httpResponseMessage.Headers.ToString(), httpResponseMessage2.Headers.ToString());
            Assert.Equal(await httpResponseMessage.Content.ReadAsStringAsync(),
              await httpResponseMessage2.Content.ReadAsStringAsync());
            Assert.Equal(httpResponseMessage.Content.Headers.ToString(),
              httpResponseMessage2.Content.Headers.ToString());
        }
    }
}
