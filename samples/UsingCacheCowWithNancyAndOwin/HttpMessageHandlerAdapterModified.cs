
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using System.Web.Http.Owin;
using System.Web.Http.Owin.Properties;
using Microsoft.Owin;
using Owin;

namespace System.Web.Http.Owin
{
    /// <summary>
    /// Represents an OWIN component that submits requests to an <see cref="HttpMessageHandler"/> when invoked.
    /// </summary>
    public class HttpMessageHandlerAdapterModified : OwinMiddleware, IDisposable
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage, bool> _callNextPolicy;
        private HttpMessageInvoker _messageInvoker;
        private IHostBufferPolicySelector _bufferPolicySelector;
        private bool _disposed;

        public HttpMessageHandlerAdapterModified(OwinMiddleware next, 
            HttpMessageHandler messageHandler,
            IHostBufferPolicySelector bufferPolicySelector)
            : this(next, messageHandler, bufferPolicySelector, DefaultCallNextPolicy)
        {
        }

        public HttpMessageHandlerAdapterModified(OwinMiddleware next,
          HttpMessageHandler messageHandler,
          IHostBufferPolicySelector bufferPolicySelector,
            Func<HttpRequestMessage, HttpResponseMessage, bool> callNextPolicy) 
            : base(next)   
        {
            _callNextPolicy = callNextPolicy;
            if (messageHandler == null)
            {
                throw new ArgumentNullException("messageHandler");
            }
            if (bufferPolicySelector == null)
            {
                throw new ArgumentNullException("bufferPolicySelector");
            }

            _messageInvoker = new HttpMessageInvoker(messageHandler);
            _bufferPolicySelector = bufferPolicySelector;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                _messageInvoker.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static bool DefaultCallNextPolicy(HttpRequestMessage request, HttpResponseMessage response)
        {
            return true;
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            IOwinRequest owinRequest = context.Request;
            IOwinResponse owinResponse = context.Response;

            if (owinRequest == null)
            {
                throw new InvalidOperationException();
            }
            if (owinResponse == null)
            {
                throw new InvalidOperationException();
            }

            HttpRequestMessage request = CreateRequestMessage(owinRequest);
            MapRequestProperties(request, context);

            if (!owinRequest.Body.CanSeek && _bufferPolicySelector.UseBufferedInputStream(hostContext: context))
            {
                await BufferRequestBodyAsync(owinRequest, request.Content);
            }

            SetPrincipal(owinRequest.User);

            HttpResponseMessage response = null;
            bool callNext = false;
            try
            {
                response = await _messageInvoker.SendAsync(request, owinRequest.CallCancelled);

                // Handle null responses
                if (response == null)
                {
                    throw new InvalidOperationException();
                }

                if (_callNextPolicy(request, response))
                {
                    callNext = true;
                }
                else
                {
                    if (response.Content != null && _bufferPolicySelector.UseBufferedOutputStream(response))
                    {
                        response = await BufferResponseBodyAsync(request, response);
                    }

                    FixUpContentLengthHeaders(response);
                    await SendResponseMessageAsync(response, owinResponse);
                }
            }
            finally
            {
                // Note that the HttpRequestMessage is explicitly NOT disposed.  Disposing it would close the input stream
                // and prevent cascaded components from accessing it.  The server MUST handle any necessary cleanup upon
                // request completion.
                request.DisposeRequestResources();
                if (response != null)
                {
                    response.Dispose();
                }
            }

            // Call the next component if no route matched
            if (callNext && Next != null)
            {
                await Next.Invoke(context);
            }

        }

        private static HttpRequestMessage CreateRequestMessage(IOwinRequest owinRequest)
        {
            // Create the request
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(owinRequest.Method), owinRequest.Uri);

            // Set the body
            HttpContent content = new StreamContent(owinRequest.Body);
            request.Content = content;

            // Copy the headers
            foreach (KeyValuePair<string, string[]> header in owinRequest.Headers)
            {
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    bool success = content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    Contract.Assert(success, "Every header can be added either to the request headers or to the content headers");
                }
            }

            return request;
        }

        // Responsible for setting Content-Length and Transfer-Encoding if needed
        private static void FixUpContentLengthHeaders(HttpResponseMessage response)
        {
            HttpContent responseContent = response.Content;
            if (responseContent != null)
            {
                if (response.Headers.TransferEncodingChunked == true)
                {
                    // According to section 4.4 of the HTTP 1.1 spec, HTTP responses that use chunked transfer
                    // encoding must not have a content length set. Chunked should take precedence over content
                    // length in this case because chunked is always set explicitly by users while the Content-Length
                    // header can be added implicitly by System.Net.Http.
                    responseContent.Headers.ContentLength = null;
                }
                else
                {
                    // Triggers delayed content-length calculations.
                    if (responseContent.Headers.ContentLength == null)
                    {
                        // If there is no content-length we can compute, then the response should use
                        // chunked transfer encoding to prevent the server from buffering the content
                        response.Headers.TransferEncodingChunked = true;
                    }
                }
            }
        }

        private static Task SendResponseMessageAsync(HttpResponseMessage response, IOwinResponse owinResponse)
        {
            owinResponse.StatusCode = (int)response.StatusCode;
            owinResponse.ReasonPhrase = response.ReasonPhrase;

            // Copy non-content headers
            IDictionary<string, string[]> responseHeaders = owinResponse.Headers;
            foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
            {
                responseHeaders[header.Key] = header.Value.AsArray();
            }

            HttpContent responseContent = response.Content;
            if (responseContent == null)
            {
                // Set the content-length to 0 to prevent the server from sending back the response chunked
                responseHeaders["Content-Length"] = new string[] { "0" };
                return TaskHelpers.Completed();
            }
            else
            {
                // Copy content headers
                foreach (KeyValuePair<string, IEnumerable<string>> contentHeader in responseContent.Headers)
                {
                    responseHeaders[contentHeader.Key] = contentHeader.Value.AsArray();
                }

                // Copy body
                return responseContent.CopyToAsync(owinResponse.Body);
            }
        }

        private static void MapRequestProperties(HttpRequestMessage request, IOwinContext context)
        {
            // Set the OWIN context on the request
            request.SetOwinContext(context);

            // Set the virtual path root for link resolution and link generation to work
            // OWIN spec requires request path base to be either the empty string or start with "/"
            string requestPathBase = context.Request.PathBase;
            request.SetVirtualPathRoot(String.IsNullOrEmpty(requestPathBase) ? "/" : requestPathBase);

            // Set a delegate to get the client certificate
            request.Properties[HttpPropertyKeys.RetrieveClientCertificateDelegateKey] = new Func<HttpRequestMessage, X509Certificate2>(
                req => context.Get<X509Certificate2>("ssl.ClientCertificate"));

            // Set a lazily-evaluated way of determining whether the request is local or not
            Lazy<bool> isLocal = new Lazy<bool>(() => context.Get<bool>("server.IsLocal"), isThreadSafe: false);
            request.Properties[HttpPropertyKeys.IsLocalKey] = isLocal;
        }

        private static async Task BufferRequestBodyAsync(IOwinRequest owinRequest, HttpContent content)
        {
            await content.LoadIntoBufferAsync();
            // We need to replace the request body with a buffered stream so that other
            // components can read the stream
            owinRequest.Body = await content.ReadAsStreamAsync();
        }


        private static async Task<HttpResponseMessage> BufferResponseBodyAsync(HttpRequestMessage request, HttpResponseMessage response)
        {
            Exception exception = null;
            try
            {
                await response.Content.LoadIntoBufferAsync();
            }
            catch (Exception e)
            {
                exception = e;
            }

            // If the content can't be buffered, create a buffered error response for the exception
            // This code will commonly run when a formatter throws during the process of serialization
            if (exception != null)
            {
                response.Dispose();
                response = request.CreateErrorResponse(HttpStatusCode.InternalServerError, exception);
                await response.Content.LoadIntoBufferAsync();
            }
            return response;
        }

        private static void SetPrincipal(IPrincipal user)
        {
            if (user != null)
            {
                Thread.CurrentPrincipal = user;
            }
        }
    }

}

namespace System.Collections.Generic
{
    /// <summary>
    /// Helper extension methods for fast use of collections.
    /// </summary>
    internal static class CollectionExtensions
    {
        /// <summary>
        /// Return a new array with the value added to the end. Slow and best suited to long lived arrays with few writes relative to reads.
        /// </summary>
        public static T[] AppendAndReallocate<T>(this T[] array, T value)
        {
            Contract.Assert(array != null);

            int originalLength = array.Length;
            T[] newArray = new T[originalLength + 1];
            array.CopyTo(newArray, 0);
            newArray[originalLength] = value;
            return newArray;
        }

        /// <summary>
        /// Return the enumerable as an Array, copying if required. Optimized for common case where it is an Array. 
        /// Avoid mutating the return value.
        /// </summary>
        public static T[] AsArray<T>(this IEnumerable<T> values)
        {
            Contract.Assert(values != null);

            T[] array = values as T[];
            if (array == null)
            {
                array = values.ToArray();
            }
            return array;
        }

        /// Sets the root virtual path associated with this request.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="virtualPathRoot">The virtual path root to associate with this request.</param>
        public static void SetVirtualPathRoot(this HttpRequestMessage request, string virtualPathRoot)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (virtualPathRoot == null)
            {
                throw new ArgumentNullException("virtualPathRoot");
            }

            request.Properties["MS_VirtualPathRoot"] = virtualPathRoot;
        }
    }
}

public static class AppBuilderExtensions
{
    private static readonly IHostBufferPolicySelector _defaultBufferPolicySelector = new OwinBufferPolicySelector();


    public static IAppBuilder UseHttpMessageHandler(this IAppBuilder builder, 
        HttpMessageHandler messageHandler,
        Func<HttpRequestMessage, HttpResponseMessage, bool> callNextPolicy)
    {
        return builder.Use(typeof(HttpMessageHandlerAdapterModified), 
            messageHandler, 
            _defaultBufferPolicySelector, 
            callNextPolicy);
    }

    public static IAppBuilder UseWebApi(this IAppBuilder builder, 
        HttpConfiguration configuration,
        Func<HttpRequestMessage, HttpResponseMessage, bool> callNextPolicy)
    {
        IHostBufferPolicySelector bufferPolicySelector = configuration.Services.GetHostBufferPolicySelector() ?? _defaultBufferPolicySelector;
        return builder.Use(typeof(HttpMessageHandlerAdapterModified), new HttpServer(configuration), bufferPolicySelector, callNextPolicy);
    }


}