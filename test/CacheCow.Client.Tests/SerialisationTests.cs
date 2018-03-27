using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Client;
using Xunit;
using CacheCow.Common;


namespace CacheCow.Client.Tests
{
	
	public class SerialisationTests
	{

		[Fact]
		public async Task Response_Deserialize_Serialize()
		{
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CacheCow.Client.Tests.Data.Response.bin");
			var serializer = new MessageContentHttpMessageSerializer();
			var response = await serializer.DeserializeToResponseAsync(stream);

			var memoryStream = new MemoryStream();
			await serializer.SerializeAsync(response, memoryStream);

			memoryStream.Position = 0;
			var response2 = await serializer.DeserializeToResponseAsync(memoryStream);
			var result = DeepComparer.Compare(response, response2);
			if(result.Count()>0)
				throw new Exception(string.Join("\r\n", result));
		}

		[Fact]
		public async Task Request_Deserialize_Serialize()
		{
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CacheCow.Client.Tests.Data.Request.bin");
			var serializer = new MessageContentHttpMessageSerializer();
			var request = await serializer.DeserializeToRequestAsync(stream);

			var memoryStream = new MemoryStream();
			await serializer.SerializeAsync(request, memoryStream);

			memoryStream.Position = 0;
			var request2 = await serializer.DeserializeToRequestAsync(memoryStream);
			var result = DeepComparer.Compare(request, request2);

			// !! Ignore this until RTM since this is fixed. See http://aspnetwebstack.codeplex.com/workitem/303
			//if (result.Count() > 0)
				//Assert.Fail(string.Join("\r\n", result));
		}

		[Fact]
		public async Task Response_Deserialize_Serialize_File()
		{
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CacheCow.Client.Tests.Data.Response.bin");
			var serializer = new MessageContentHttpMessageSerializer();
			var response = await serializer.DeserializeToResponseAsync(stream);

			using(var fileStream = new FileStream(Path.GetTempFileName(), FileMode.Create))
			{
				await serializer.SerializeAsync(response, fileStream);

				fileStream.Position = 0;
				var response2 = await serializer.DeserializeToResponseAsync(fileStream);
				var result = DeepComparer.Compare(response, response2);
				if (result.Count() > 0)
					throw new Exception(string.Join("\r\n", result));
			}
		}

		[Fact]
		public async Task Request_Deserialize_Serialize_File()
		{
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CacheCow.Client.Tests.Data.Request.bin");
			var serializer = new MessageContentHttpMessageSerializer();
			var request = await serializer.DeserializeToRequestAsync(stream);

			using(var fileStream = new FileStream(Path.GetTempFileName(), FileMode.Create))
			{
				await serializer.SerializeAsync(request, fileStream);

				fileStream.Position = 0;
				var request2 = await serializer.DeserializeToRequestAsync(fileStream);
				var result = DeepComparer.Compare(request, request2);

				if (result.Count() > 0)
				    throw new Exception(string.Join("\r\n", result));
			}
		}
	

	}
}
