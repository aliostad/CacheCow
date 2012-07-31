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
	/// Default implementation of IHttpMessageSerializer using proprietry format
	/// Does not close the stream since the stream can be used to store other objects
	/// so it has to be closed in the client
	/// </summary>
	public class MessageContentHttpMessageSerializer : IHttpMessageSerializer
	{
		private bool _bufferContent;

		public MessageContentHttpMessageSerializer() : this(false)
		{
			
		}

		public MessageContentHttpMessageSerializer(bool bufferContent)
		{
			_bufferContent = bufferContent;
		}


		public void Serialize(HttpResponseMessage response, Stream stream)
		{
			byte[] assuranceBuffer = null;
			if (_bufferContent && response.Content != null)
				assuranceBuffer = response.Content.ReadAsByteArrayAsync().Result; // make sure it is buffered

			var httpMessageContent = new HttpMessageContent(response);
			var buffer = httpMessageContent.ReadAsByteArrayAsync().Result;
			stream.Write(buffer, 0, buffer.Length);
		}

		public void Serialize(HttpRequestMessage request, Stream stream)
		{
			byte[] assuranceBuffer = null;
			if (_bufferContent && request.Content != null)
				assuranceBuffer = request.Content.ReadAsByteArrayAsync().Result; // make sure it is buffered

			var httpMessageContent = new HttpMessageContent(request);
			var buffer = httpMessageContent.ReadAsByteArrayAsync().Result;
			stream.Write(buffer, 0, buffer.Length);
		}

		public HttpResponseMessage DeserializeToResponse(Stream stream)
		{
			var response = new HttpResponseMessage();
			var memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			response.Content = new ByteArrayContent(memoryStream.ToArray());
			response.Content.Headers.Add("Content-Type", "application/http;msgtype=response");
			return response.Content.ReadAsHttpResponseMessageAsync().Result;
		}

		public HttpRequestMessage DeserializeToRequest(Stream stream)
		{
			var request = new HttpRequestMessage();
			var memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			request.Content = new ByteArrayContent(memoryStream.ToArray());
			request.Content.Headers.Add("Content-Type", "application/http;msgtype=request");
			return request.Content.ReadAsHttpRequestMessageAsync().Result;
		}
	}
}
