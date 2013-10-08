using System;
using System.Collections.Generic;
using System.Linq;
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

        public HttpSelfHostConfiguration Configuration
        {
            get { return _configuration; }
        }

        public void Start()
        {

            _configuration = new HttpSelfHostConfiguration(TestConstants.BaseUrl);
            WebApiConfig.Register(Configuration);
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

        public void Dispose()
        {
            Stop();
        }
    }
}
