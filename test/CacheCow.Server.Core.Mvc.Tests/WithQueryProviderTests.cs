using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace CacheCow.Server.Core.Mvc.Tests
{
    public class WithQueryProviderTests
    {
        private TestServer _server;
        private HttpClient _client;

        public WithQueryProviderTests()
        {
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<WithQueryProviderStartup>());
            _client = _server.CreateClient();
        }

    }

    public class WithQueryProviderStartup
    {
        public WithQueryProviderStartup(IConfiguration configuration)
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
