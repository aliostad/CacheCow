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
    public class SimpleHashBasedHttpCacheFilterTests
    {

        private TestServer _server;
        private HttpClient _client;

        public SimpleHashBasedHttpCacheFilterTests()
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
            Assert.NotNull(response.Headers.ETag);
        }

        [Fact]
        public async Task ETagNotWeek()
        {
            var response = await _client.GetAsync("/api/test/1");
            Assert.NotNull(response.Headers.ETag);
            Assert.NotNull(response.Headers.ETag);
            Assert.False(response.Headers.ETag.IsWeak);
        }

        [Fact]
        public async Task HashingReturnsTheSameValue()
        {
            var response = await _client.GetAsync("/api/test/1");
            var response2 = await _client.GetAsync("/api/test/1");
            Assert.NotNull(response.Headers.ETag);
            Assert.NotNull(response2.Headers.ETag);
            Assert.Equal(response.Headers.ETag.Tag, response2.Headers.ETag.Tag);
            Assert.Equal(response.Headers.ETag.IsWeak, response2.Headers.ETag.IsWeak);
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
