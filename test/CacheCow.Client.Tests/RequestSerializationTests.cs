using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Client;
using CacheCow.Common;
using Xunit;

namespace CacheCow.Client.Tests
{
	
	public class RequestSerializationTests
	{
		[Fact]
		public async Task IntegrationTest_Serialize()
		{
			var requestMessage = new HttpRequestMessage( HttpMethod.Get, "http://some.server/api/foo");
			requestMessage.Headers.Range = new RangeHeaderValue(0, 1) { Unit = "custom" };
			var serializer = new MessageContentHttpMessageSerializer();
			var memoryStream = new MemoryStream();
			await serializer.SerializeAsync(requestMessage, memoryStream);
			memoryStream.Position = 0;
			var request = await serializer.DeserializeToRequestAsync(memoryStream);
			Assert.Equal(requestMessage.Headers.Range.Unit, request.Headers.Range.Unit);
		}
	}
}
