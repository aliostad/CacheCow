using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            : this(true)
        {

        }

        public MessageContentHttpMessageSerializer(bool bufferContent)
        {
            _bufferContent = bufferContent;
        }

        public async Task SerializeAsync(HttpResponseMessage response, Stream stream)
        {
            if (response.Content != null)
            {
                TraceWriter.WriteLine("SerializeAsync - before load",
                    TraceLevel.Verbose);
                if(_bufferContent)
                    await response.Content.LoadIntoBufferAsync();
                TraceWriter.WriteLine("SerializeAsync - after load", TraceLevel.Verbose);               
            }
            else
            {
                TraceWriter.WriteLine("Content NULL - before load",
                    TraceLevel.Verbose);
            }

            var httpMessageContent = new HttpMessageContent(response);
            var buffer = await httpMessageContent.ReadAsByteArrayAsync();
            TraceWriter.WriteLine("SerializeAsync - after ReadAsByteArrayAsync", TraceLevel.Verbose);
            await Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite,
                buffer, 0, buffer.Length, null, TaskCreationOptions.AttachedToParent);
        }

        public async Task SerializeAsync(HttpRequestMessage request, Stream stream)
        {
            if (request.Content != null && _bufferContent)
            {
                await request.Content.LoadIntoBufferAsync();
            }

            var httpMessageContent = new HttpMessageContent(request);
            // All in-memory and CPU-bound so no need to async
            var buffer = await httpMessageContent.ReadAsByteArrayAsync();
            await Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite,
                buffer, 0, buffer.Length, null, TaskCreationOptions.AttachedToParent);
        }

        public async Task<HttpResponseMessage> DeserializeToResponseAsync(Stream stream)
        {
            var response = new HttpResponseMessage();
            response.Content = new StreamContent(stream);
            response.Content.Headers.Add("Content-Type", "application/http;msgtype=response");
            TraceWriter.WriteLine("before ReadAsHttpResponseMessageAsync",
                    TraceLevel.Verbose);
            var responseMessage = await response.Content.ReadAsHttpResponseMessageAsync();
            if (responseMessage.Content != null && _bufferContent)
                await responseMessage.Content.LoadIntoBufferAsync();
            return responseMessage;
        }

        public async Task<HttpRequestMessage> DeserializeToRequestAsync(Stream stream)
        {
            var request = new HttpRequestMessage();
            request.Content = new StreamContent(stream);
            request.Content.Headers.Add("Content-Type", "application/http;msgtype=request");
            var requestMessage = await request.Content.ReadAsHttpRequestMessageAsync();
            if (requestMessage.Content != null && _bufferContent)
                await requestMessage.Content.LoadIntoBufferAsync();
            return requestMessage;
        }
    }
}
