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

namespace CacheCow.Server.Core.Mvc.Tests
{
    public class HttpCacheFilterTests
    {

        private TestServer _server;
        private HttpClient _client;

        public HttpCacheFilterTests()
        {
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<HttpCacheFilterTestsStartup>());
            _client = _server.CreateClient();
        }

        [Fact]
        public async Task ForSimpleGetYouGetCacheControlBack()
        {
            var response = await _client.GetAsync("/api/test/1");
            var viewModel = await response.Content.ReadAsAsync<TestViewModel>();
            Assert.NotNull(response.Headers.CacheControl);
            Assert.NotNull(response.Headers.ETag);

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
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddHttpCaching();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "api",
                    defaults: new { action = "GET" },
                    template: "api/{controller}/{id?}");
            });

        }

    }
}
