using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace CacheCow.IntegrationTesting.Server
{
    class InMemoryServer : IDisposable
    {
        private HttpSelfHostServer _server;
        private HttpSelfHostConfiguration _configuration;
        private CaptureMessagesDelegatingHandler _sniffer;
        public HttpSelfHostConfiguration Configuration
        {
            get { return _configuration; }
        }

        public void Start()
        {

            _configuration = new HttpSelfHostConfiguration(TestConstants.BaseUrl);
            WebApiConfig.Register(Configuration);
            _sniffer = new CaptureMessagesDelegatingHandler();
            _configuration.MessageHandlers.Insert(0, _sniffer);
            _server = new HttpSelfHostServer(Configuration);
            _server.OpenAsync().Wait(); // yeah this looks bad ... :)
        }

        public void Stop()
        {
            if (_server != null)
            {
                _server.CloseAsync().Wait();
                _server.Dispose();
                _server = null;
            }
        }

        public HttpResponseMessage LastResponse
        {
            get { return _sniffer.LastResponse; }
        }

        public HttpRequestMessage LastRequest
        {
            get { return _sniffer.LastRequest; }
        }

        public void Dispose()
        {
            Stop();
        }

        private class CaptureMessagesDelegatingHandler : DelegatingHandler
        {

            private HttpRequestMessage _lastRequest;
            private HttpResponseMessage _lastResponse;

            public HttpRequestMessage LastRequest
            {
                get { return _lastRequest; }
            }

            public HttpResponseMessage LastResponse
            {
                get { return _lastResponse; }
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                _lastRequest = request;
                return base.SendAsync(request, cancellationToken)
                    .ContinueWith(t =>
                        {
                            _lastResponse = t.Result;
                            return t.Result;
                        });
            }
        }

    }
}
