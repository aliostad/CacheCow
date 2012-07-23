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
	}
}
