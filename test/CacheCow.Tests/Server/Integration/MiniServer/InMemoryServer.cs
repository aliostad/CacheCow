using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace CacheCow.Tests.Server.Integration.MiniServer
{
    public class InMemoryServer: HttpMessageHandler
    {
        private HttpServer _httpServer;
        private HttpMessageInvoker _invoker;

        public InMemoryServer(HttpConfiguration configuration)
        {
            _httpServer = new HttpServer(configuration);
            _invoker = new HttpMessageInvoker(_httpServer);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _invoker.SendAsync(request, cancellationToken);
        }
    }
}
