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
            using (var stream = new FileStream("Data/Response.cs", FileMode.Open))
            {
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
		}

		[Fact]
		public async Task Request_Deserialize_Serialize()
		{
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CacheCow.Client.Tests.Data.Request.cs");
			var serializer = new MessageContentHttpMessageSerializer();
			var request = await serializer.DeserializeToRequestAsync(stream);

			var memoryStream = new MemoryStream();
			await serializer.SerializeAsync(request, memoryStream);

			memoryStream.Position = 0;
			var request2 = await serializer.DeserializeToRequestAsync(memoryStream);
			var result = DeepComparer.Compare(request, request2);

		}

		[Fact]
		public async Task Response_Deserialize_Serialize_File()
		{
            using (var stream = new FileStream("Data/Response.cs", FileMode.Open))
            {
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
        }

		[Fact]
		public async Task Request_Deserialize_Serialize_File()
		{
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CacheCow.Client.Tests.Data.Request.cs");
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
        // temporarily remove this test || NETCOREAPP2_0
#if NET462

        // Issue raised here https://github.com/dotnet/corefx/issues/31918
        [Fact]
        public async Task Issue31918_On_Net_Framework()
        {
            var serializer = new MessageContentHttpMessageSerializer();
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://google.com"));
            var response = await client.SendAsync(request);
            var ms = new MemoryStream();
            await serializer.SerializeAsync(response, ms);
            ms.Position = 0;
            var r2 = await serializer.DeserializeToResponseAsync(ms);
            Console.WriteLine(response);
        }

#endif

	}
}
