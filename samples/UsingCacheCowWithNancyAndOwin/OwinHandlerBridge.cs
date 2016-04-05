using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace UsingCacheCowWithNancyAndOwin
{
    public class OwinHandlerBridge : HttpMessageHandler
    {
        private readonly DelegatingHandler _delegatingHandler;
        private readonly FixedResponseHandler _fixedResponseHandler = new FixedResponseHandler();
        private readonly HttpMessageInvoker _invoker;

        private const string OwinHandlerBridgeResponse = "OwinHandlerBridge_Response";

        private class FixedResponseHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
                CancellationToken cancellationToken)
            {
                return Task.FromResult(
                    (HttpResponseMessage)
                    request.Properties[OwinHandlerBridgeResponse]);
            }

        }

        public OwinHandlerBridge(DelegatingHandler delegatingHandler)
        {
            _delegatingHandler = delegatingHandler;
            _delegatingHandler.InnerHandler = _fixedResponseHandler;
            _invoker = new HttpMessageInvoker(_delegatingHandler);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            var owinContext = request.GetOwinContext();
            request.Properties.Add(OwinHandlerBridgeResponse, owinContext.Response.ToHttpResponseMessage());
            return _invoker.SendAsync(request, cancellationToken);
        }
    }
}