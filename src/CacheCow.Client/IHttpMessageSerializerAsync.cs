using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CacheCow.Client
{
	public interface IHttpMessageSerializerAsync
	{
		Task SerializeAsync(Task<HttpResponseMessage> response, Stream stream);
		Task SerializeAsync(HttpRequestMessage request, Stream stream);
		Task<HttpResponseMessage> DeserializeToResponseAsync(Stream stream);
		Task<HttpRequestMessage> DeserializeToRequestAsync(Stream stream);
	}
}
