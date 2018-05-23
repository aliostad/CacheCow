using CacheCow.Samples.Common;
using CacheCow.Server;
using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Samples.Carter
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCarter();
            services.AddHttpCaching();
            services.AddSingleton<ICarRepository>(InMemoryCarRepository.Instance);
            services.AddQueryProviderForViewModel<Car, TimedETagQueryCarRepository>(false);
            services.AddQueryProviderForViewModel<IEnumerable<Car>, TimedETagQueryCarRepository>(false);
            services.Remove<ITimedETagQueryProvider>();
            services.AddSingleton<ITimedETagQueryProvider, TimedETagQueryCarRepository>();
            services.AddSingleton<ITimedETagExtractor<IEnumerable<Car>>, CarCollectionETagExtractor>();
            services.AddSingleton<ITimedETagExtractor<Car>, CarETagExtractor>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCarter();
        }
    }
}
