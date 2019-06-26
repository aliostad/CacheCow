using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Xunit;
using CacheCow.Server.Headers;


namespace CacheCow.Server.WebApi.Tests
{
    public class IntegrationTests
    {
        private readonly HttpServer _server;

        private HttpConfiguration GetConfig()
        {
            var config = new HttpConfiguration();
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.Routes.MapHttpRoute(name: "Default", routeTemplate: "api/{controller}/{id}", defaults: new { id = RouteParameter.Optional });
            return config;
        }

        public IntegrationTests()
        {
            _server = new HttpServer(GetConfig());
        }

        [Fact]
        public async Task HasHeaders()
        {
            var i = new HttpMessageInvoker(_server);
            var response  = await i.SendAsync(new HttpRequestMessage(HttpMethod.Get, new Uri("http://chiz/api/car/1", UriKind.Absolute)), CancellationToken.None);
            Assert.True(response.IsSuccessStatusCode);
            var h = response.GetCacheCowHeader();
            Assert.NotNull(h);
        }

        [Fact]
        public async Task Issue232_CanHandleExceptionInController()
        {
            var i = new HttpMessageInvoker(_server);
            var req = new HttpRequestMessage(HttpMethod.Get, new Uri("http://chiz/api/car/42", UriKind.Absolute));
            var resp = await i.SendAsync(req, CancellationToken.None);
            var error = await resp.Content.ReadAsStringAsync();
            Assert.Contains("MeaningOfLife", error);
        }

    }
}
