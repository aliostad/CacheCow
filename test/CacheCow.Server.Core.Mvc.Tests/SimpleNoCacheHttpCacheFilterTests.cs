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
using System.Reflection;

namespace CacheCow.Server.Core.Mvc.Tests
{
    public class SimpleNoCacheHttpCacheFilterTests
    {

        private TestServer _server;
        private HttpClient _client;
        private IConfiguration _config;

        private void CreateClientAndServer()
        {
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<HttpCacheFilterTestsStartup>()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                    config.AddEnvironmentVariables();
                })
            );

            _client = _server.CreateClient();

        }

        [Fact]
        public async Task ForSimpleGetYouGetCacheControlAndBack()
        {
            CreateClientAndServer();
            var response = await _client.GetAsync("/api/test/1");
            var viewModel = await response.Content.ReadAsAsync<TestViewModel>();
            Assert.NotNull(response.Headers.CacheControl);
        }

        [Fact]
        public async Task ConfigurationOverrides()
        {
            CreateClientAndServer();
            var response = await _client.GetAsync("/api/test/1");
            var viewModel = await response.Content.ReadAsAsync<TestViewModel>();
            Assert.NotNull(response.Headers.CacheControl);
            Assert.Equal(TimeSpan.FromSeconds(10), response.Headers.CacheControl.MaxAge);
        }

        [Fact]
        public async Task EnvironmentVariablesOverridConfiguration()
        {
            Environment.SetEnvironmentVariable("CacheCow:Test:GET:Expiry" , "00:00:11",
                EnvironmentVariableTarget.Process);
            CreateClientAndServer();
            var response = await _client.GetAsync("/api/test/1");
            var viewModel = await response.Content.ReadAsAsync<TestViewModel>();
            Assert.NotNull(response.Headers.CacheControl);
            Assert.Equal(TimeSpan.FromSeconds(11), response.Headers.CacheControl.MaxAge);
        }

        [Fact]
        public async Task Issue232_CanHandleExceptionThrownFromTheController()
        {
            CreateClientAndServer();
            await Assert.ThrowsAsync<MeaningOfLifeException>(() => _client.GetAsync("/api/test/42"));
        }

        [Fact]
        public async Task NoETagSinceNoCachingForGetAll()
        {
            CreateClientAndServer();
            var response = await _client.GetAsync("/api/test");
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
            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
            });
            services.AddHttpCachingMvc(options =>
            {
                options.EnableConfiguration = true;
            });
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
