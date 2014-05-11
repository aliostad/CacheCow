using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Client;
using NUnit.Framework;
using CacheCow.Common;


namespace CacheCow.Client.Tests
{
	[TestFixture]
	public class SerialisationTests
	{


		[Test]
		public void Response_Deserialize_Serialize()
		{
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CacheCow.Client.Tests.Data.Response.cs");
			var serializer = new MessageContentHttpMessageSerializer();
			var response = serializer.DeserializeToResponseAsync(stream).Result;

			var memoryStream = new MemoryStream();
			serializer.SerializeAsync(TaskHelpers.FromResult(response), memoryStream).Wait();

			memoryStream.Position = 0;
			var response2 = serializer.DeserializeToResponseAsync(memoryStream).Result;
			var result = DeepComparer.Compare(response, response2);
			if(result.Count()>0)
				Assert.Fail(string.Join("\r\n", result));
		}

		[Test]
		public void Request_Deserialize_Serialize()
		{
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CacheCow.Client.Tests.Data.Request.cs");
			var serializer = new MessageContentHttpMessageSerializer();
			var request = serializer.DeserializeToRequestAsync(stream).Result;

			var memoryStream = new MemoryStream();
			serializer.SerializeAsync(request, memoryStream).Wait();

			memoryStream.Position = 0;
			var request2 = serializer.DeserializeToRequestAsync(memoryStream).Result;
			var result = DeepComparer.Compare(request, request2);

			// !! Ignore this until RTM since this is fixed. See http://aspnetwebstack.codeplex.com/workitem/303
			//if (result.Count() > 0)
				//Assert.Fail(string.Join("\r\n", result));
		}

		[Test]
		public void Response_Deserialize_Serialize_File()
		{
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CacheCow.Client.Tests.Data.Response.cs");
			var serializer = new MessageContentHttpMessageSerializer();
			var response = serializer.DeserializeToResponseAsync(stream).Result;

			using(var fileStream = new FileStream("response.tmp", FileMode.Create))
			{
				serializer.SerializeAsync(TaskHelpers.FromResult(response), fileStream).Wait();

				fileStream.Position = 0;
				var response2 = serializer.DeserializeToResponseAsync(fileStream).Result;
				var result = DeepComparer.Compare(response, response2);
				if (result.Count() > 0)
					Assert.Fail(string.Join("\r\n", result));
			}
		}

		[Test]
		public void Request_Deserialize_Serialize_File()
		{
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CacheCow.Client.Tests.Data.Request.cs");
			var serializer = new MessageContentHttpMessageSerializer();
			var request = serializer.DeserializeToRequestAsync(stream).Result;

			using(var fileStream = new FileStream("request.tmp", FileMode.Create))
			{
				serializer.SerializeAsync(request, fileStream).Wait();

				fileStream.Position = 0;
				var request2 = serializer.DeserializeToRequestAsync(fileStream).Result;
				var result = DeepComparer.Compare(request, request2);

				if (result.Count() > 0)
				Assert.Fail(string.Join("\r\n", result));
			}
		}
	

	}
}
