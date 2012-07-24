using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Client
{
	/// <summary>
	/// Default implementation of IHttpResponseMessageSerializer using proprietry format
	/// Does not close the stream since the stream can be used to store other objects
	/// so it has to be closed in the client
	/// </summary>
	public class DefaultHttpResponseMessageSerializer : IHttpResponseMessageSerializer
	{

		private const int FourByteId = 0x73BAB140;

		public void Serialize(HttpResponseMessage response, Stream stream)
		{
			var httpMessageContent = new HttpMessageContent(response);
			var buffer = httpMessageContent.ReadAsByteArrayAsync().Result;
			stream.Write(buffer, 0, buffer.Length);
		}

		public HttpResponseMessage Deserialize(Stream stream)
		{
			var response = new HttpResponseMessage();
			var memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			response.Content = new ByteArrayContent(memoryStream.ToArray());
			response.Content.Headers.Add("Content-Type", "application/http;msgtype=response");
			return response.Content.ReadAsHttpResponseMessageAsync().Result;
		}
	}
}
