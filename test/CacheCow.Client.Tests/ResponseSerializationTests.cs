using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Client;
using CacheCow.Common;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CacheCow.Client.Tests
{
	[TestFixture]
	public class ResponseSerializationTests
	{
		[Test]
		[Ignore]
		public void IntegrationTest_Serialize()
		{
			var httpClient = new HttpClient();
			var httpResponseMessage = httpClient.GetAsync("http://google.com").Result;
			Console.WriteLine(httpResponseMessage.Headers.ToString());
			var defaultHttpResponseMessageSerializer = new MessageContentHttpMessageSerializer();
			var fileStream = new FileStream("msg.bin", FileMode.Create);
			defaultHttpResponseMessageSerializer.SerializeAsync(TaskHelpers.FromResult(httpResponseMessage), fileStream).Wait();
			fileStream.Close();
		}

		[Test]
		[Ignore]
		public void IntegrationTest_Deserialize()
		{	var fileStream = new FileStream("msg.bin", FileMode.Open);
			var defaultHttpResponseMessageSerializer = new MessageContentHttpMessageSerializer();
			var httpResponseMessage = defaultHttpResponseMessageSerializer.DeserializeToResponseAsync(fileStream).Result;
			fileStream.Close();
		}

		[Test]
		[Ignore]
		public void IntegrationTest_Serialize_Deserialize()
		{
			
			var httpClient = new HttpClient();
			var httpResponseMessage = httpClient.GetAsync("http://google.com").Result;
			var contentLength = httpResponseMessage.Content.Headers.ContentLength; // access to make sure is populated http://aspnetwebstack.codeplex.com/discussions/388196
			var memoryStream = new MemoryStream();
			var defaultHttpResponseMessageSerializer = new MessageContentHttpMessageSerializer();
			defaultHttpResponseMessageSerializer.SerializeAsync(TaskHelpers.FromResult(httpResponseMessage), memoryStream).Wait();
			memoryStream.Position = 0;
			var httpResponseMessage2 = defaultHttpResponseMessageSerializer.DeserializeToResponseAsync(memoryStream).Result;
			Assert.AreEqual(httpResponseMessage.StatusCode, httpResponseMessage2.StatusCode, "StatusCode");
			Assert.AreEqual(httpResponseMessage.ReasonPhrase, httpResponseMessage2.ReasonPhrase, "ReasonPhrase");
			Assert.AreEqual(httpResponseMessage.Version, httpResponseMessage2.Version, "Version");
			Assert.AreEqual(httpResponseMessage.Headers.ToString(), httpResponseMessage2.Headers.ToString(), "Headers.ToString()");
			Assert.AreEqual(httpResponseMessage.Content.ReadAsStringAsync().Result, 
				httpResponseMessage2.Content.ReadAsStringAsync().Result, "Content");
			Assert.AreEqual(httpResponseMessage.Content.Headers.ToString(),
				httpResponseMessage2.Content.Headers.ToString(), "Headers.ToString()");

		}





	}
}
