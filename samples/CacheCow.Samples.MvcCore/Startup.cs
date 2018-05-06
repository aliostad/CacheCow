using CacheCow.Samples.Common;
using CacheCow.Server;
using CacheCow.Server.Core;
using CacheCow.Server.Core.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Samples.MvcCore
{
    class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddWebApiConventions();
            services.AddHttpCaching();
            services.AddQueryProviderForViewModel<Car, TimedETagQueryCarRepository>(false);
            services.AddQueryProviderForViewModel<IEnumerable<Car>, TimedETagQueryCarRepository>(false);
            services.AddSingleton<ICarRepository>(InMemoryCarRepository.Instance);
            services.AddSingleton<ITimedETagExtractor<IEnumerable<Car>>, CarCollectionETagExtractor>();
            services.AddSingleton<ITimedETagExtractor<Car>, CarETagExtractor>();
        }

        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc(routes =>
            {

                routes.MapRoute(
                    name: "api-createorlist",
                    template: "api/{controller}");

                routes.MapRoute(
                    name: "api",
                    template: "api/{controller}/{id:int}");

            });
        }
    }
}
