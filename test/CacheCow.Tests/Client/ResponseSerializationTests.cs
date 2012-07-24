using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Client;
using NUnit.Framework;

namespace CacheCow.Tests.Client
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
			var defaultHttpResponseMessageSerializer = new DefaultHttpResponseMessageSerializer();
			var fileStream = new FileStream("msg.bin", FileMode.Create);
			defaultHttpResponseMessageSerializer.Serialize(httpResponseMessage, fileStream);
			fileStream.Close();
		}

		[Test]
		[Ignore]
		public void IntegrationTest_Deserialize()
		{	var fileStream = new FileStream("msg.bin", FileMode.Open);
			var defaultHttpResponseMessageSerializer = new DefaultHttpResponseMessageSerializer();
			var httpResponseMessage = defaultHttpResponseMessageSerializer.Deserialize(fileStream);
			fileStream.Close();
		}

		[Test]
		[Ignore]
		public void IntegrationTest_Serialize_Deserialize()
		{
			var httpClient = new HttpClient();
			var httpResponseMessage = httpClient.GetAsync("http://google.com").Result;
			var memoryStream = new MemoryStream();
			var defaultHttpResponseMessageSerializer = new DefaultHttpResponseMessageSerializer();
			defaultHttpResponseMessageSerializer.Serialize(httpResponseMessage, memoryStream);
			memoryStream.Position = 0;
			var httpResponseMessage2 = defaultHttpResponseMessageSerializer.Deserialize(memoryStream);
			Assert.AreEqual(httpResponseMessage.StatusCode, httpResponseMessage2.StatusCode, "StatusCode");
			Assert.AreEqual(httpResponseMessage.ReasonPhrase, httpResponseMessage2.ReasonPhrase, "ReasonPhrase");
			Assert.AreEqual(httpResponseMessage.Version, httpResponseMessage2.Version, "Version");
			Assert.AreEqual(httpResponseMessage.Headers.ToString(), httpResponseMessage2.Headers.ToString(), "Headers.ToString()");
			Assert.AreEqual(httpResponseMessage.Content.ReadAsStringAsync().Result, 
				httpResponseMessage2.Content.ReadAsStringAsync().Result, "Content");

		}





	}
}
