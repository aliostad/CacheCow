using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using System;
using CacheCow.Client;
using System.Net;

namespace CacheCow.Server.Core.Mvc.Tests
{
    public class SimpleNoCacheHttpCacheFilterTests
    {

        private TestServer _server;
        private HttpClient _client;

        public SimpleNoCacheHttpCacheFilterTests()
        {
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<HttpCacheFilterTestsStartup>());
            _client = _server.CreateClient();
        }

        [Fact]
        public async Task ForSimpleGetYouGetCacheControlAndBack()
        {
            var response = await _client.GetAsync("/api/test/1");
            var viewModel = await response.Content.ReadAsAsync<TestViewModel>();
            Assert.NotNull(response.Headers.CacheControl);
        }

        [Fact]
        public async Task NoETagSinceNoCaching()
        {
            var response = await _client.GetAsync("/api/test/1");
            Assert.Null(response.Headers.ETag);
        }
    }

    public class HttpCacheFilterTestsStartup
    {
        public HttpCacheFilterTestsStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddHttpCaching();
        }

        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "api-getall",
                    defaults: new { action = "GETALL" },
                    template: "api/{controller}/");

                routes.MapRoute(
                    name: "api-get",
                    defaults: new { action = "GET" },
                    template: "api/{controller}/{id:int}");               
            });

        }

    }
}
