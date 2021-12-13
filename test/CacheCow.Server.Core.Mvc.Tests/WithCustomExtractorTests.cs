using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using CacheCow.Client;
using CacheCow.Client.Headers;
using CacheCow.Server.Headers;
using Xunit;
using System.Threading.Tasks;

namespace CacheCow.Server.Core.Mvc.Tests
{
    public class WithCustomExtractorTests
    {
        private TestServer _server;
        private HttpClient _client;

        public WithCustomExtractorTests()
        {
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<WithCustomExtractorStartup>());
            _client = _server.CreateClient();
        }

        [Fact]
        public async Task SecondTimeComesFromCacheBecauseOfExtractor()
        {
            var handler = _server.CreateHandler();
            var client = ClientExtensions.CreateClient(handler);
            client.BaseAddress = _server.BaseAddress;
            var response = await client.GetAsync("/api/withquery");
            var response2 = await client.GetAsync("/api/withquery");
            var serverCch = response2.GetCacheCowHeader();
            Assert.NotNull(serverCch);
            Assert.False(serverCch.ShortCircuited);

            var cch = response2.Headers.GetCacheCowHeader();
            Assert.NotNull(cch);
            Assert.False(cch.DidNotExist);
            Assert.True(cch.CacheValidationApplied);
            Assert.True(cch.RetrievedFromCache);
        }
    }
    public class TestViewModelCollectionExtractor : ITimedETagExtractor<IEnumerable<TestViewModel>>
    {
        public TimedEntityTagHeaderValue Extract(IEnumerable<TestViewModel> viewModel)
        {
            if (viewModel == null)
                return null;
            var max = viewModel.Aggregate((a, b) => a.LastModified > b.LastModified ? a : b);
            return new TimedEntityTagHeaderValue(max.LastModified);
        }

        public TimedEntityTagHeaderValue Extract(object viewModel)
        {
            return Extract(viewModel as IEnumerable<TestViewModel>);
        }
    }

    public class WithCustomExtractorStartup : WithQueryProviderStartup
    {

        // This method gets called by the runtime. Use this method to add services to the container.
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
            });
            services.AddHttpCachingMvc();
            services.AddExtractorForViewModelMvc<IEnumerable<TestViewModel>, TestViewModelCollectionExtractor>();
        }

    }
}
