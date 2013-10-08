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

        public void Start()
        {

            var configuration = new HttpSelfHostConfiguration(TestConstants.BaseUrl);
            WebApiConfig.Register(configuration);
            _server = new HttpSelfHostServer(configuration);
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
