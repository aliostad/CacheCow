using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Common;

namespace CacheCow.Client
{
	/// <summary>
	/// Default implementation of IHttpMessageSerializer using proprietry format
	/// Does not close the stream since the stream can be used to store other objects
	/// so it has to be closed in the client
	/// </summary>
	public class MessageContentHttpMessageSerializer : IHttpMessageSerializerAsync
	{
		private bool _bufferContent;

		public MessageContentHttpMessageSerializer()
			: this(false)
		{

		}

		public MessageContentHttpMessageSerializer(bool bufferContent)
		{
			_bufferContent = bufferContent;
		}

		public Task SerializeAsync(Task<HttpResponseMessage> response, Stream stream)
		{
			return response.Then(r =>
			{
				if (r.Content != null)
				{
					return r.Content.LoadIntoBufferAsync()
						.Then(() =>
						{
							var httpMessageContent = new HttpMessageContent(r);
							// All in-memory and CPU-bound so no need to async
							var buffer = httpMessageContent.ReadAsByteArrayAsync().Result;
							return Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite,
								buffer, 0, buffer.Length, null, TaskCreationOptions.None);
						});
				}
				else
				{
					var httpMessageContent = new HttpMessageContent(r);
					// All in-memory and CPU-bound so no need to async
					var buffer = httpMessageContent.ReadAsByteArrayAsync().Result;
					return Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite,
						buffer, 0, buffer.Length, null, TaskCreationOptions.None);
				}
			}
				);
		}

		public Task SerializeAsync(HttpRequestMessage request, Stream stream)
		{
			if (request.Content != null)
			{
				return request.Content.LoadIntoBufferAsync()
					.Then(() =>
					{
						var httpMessageContent = new HttpMessageContent(request);
						// All in-memory and CPU-bound so no need to async
						var buffer = httpMessageContent.ReadAsByteArrayAsync().Result;
						return Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite,
							buffer, 0, buffer.Length, null, TaskCreationOptions.None);
					});
			}
			else
			{
				var httpMessageContent = new HttpMessageContent(request);
				// All in-memory and CPU-bound so no need to async
				var buffer = httpMessageContent.ReadAsByteArrayAsync().Result;
				return Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite,
					buffer, 0, buffer.Length, null, TaskCreationOptions.None);
			}

		}

		public Task<HttpResponseMessage> DeserializeToResponseAsync(Stream stream)
		{
			var response = new HttpResponseMessage();
			response.Content = new StreamContent(stream);
			response.Content.Headers.Add("Content-Type", "application/http;msgtype=response");
			return response.Content.ReadAsHttpResponseMessageAsync();
		}

		public Task<HttpRequestMessage> DeserializeToRequestAsync(Stream stream)
		{
			var request = new HttpRequestMessage();
			request.Content = new StreamContent(stream);
			request.Content.Headers.Add("Content-Type", "application/http;msgtype=request");
			return request.Content.ReadAsHttpRequestMessageAsync();
		}
	}
}
