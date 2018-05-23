using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using CacheCow.Client.Headers;
using CacheCow.Client;
using CacheCow.Server.Headers;
using Microsoft.AspNetCore.Http;

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

        [Fact]
        public async Task ForSimpleGetYouGetCacheControlAndETagBack()
        {
            var response = await _client.GetAsync("/api/withquery/1");
            var viewModel = await response.Content.ReadAsAsync<TestViewModel>();
            Assert.NotNull(response.Content);
            Assert.NotNull(response.Headers.ETag);
            Assert.NotNull(response.Headers.ETag.Tag);
        }

        [Fact]
        public async Task ETagsAreTheSame()
        {
            var response = await _client.GetAsync("/api/withquery/1");
            var response2 = await _client.GetAsync("/api/withquery/1");
            Assert.NotNull(response.Headers.ETag);
            Assert.NotNull(response.Headers.ETag.Tag);
            Assert.NotNull(response2.Headers.ETag);
            Assert.NotNull(response2.Headers.ETag.Tag);
            Assert.Equal(response.Headers.ETag.Tag, response2.Headers.ETag.Tag);
        }

        [Fact]
        public async Task ETagsAreTheSameForCollection()
        {
            var response = await _client.GetAsync("/api/withquery");
            var response2 = await _client.GetAsync("/api/withquery");
            Assert.NotNull(response.Headers.ETag);
            Assert.NotNull(response.Headers.ETag.Tag);
            Assert.NotNull(response2.Headers.ETag);
            Assert.NotNull(response2.Headers.ETag.Tag);
            Assert.Equal(response.Headers.ETag.Tag, response2.Headers.ETag.Tag);
        }

        [Fact]
        public async Task SecondTimeComesFromCacheBecauseOfQueryProvider()
        {
            var ETagThatHashingGenerates = "U/I5fgFOxAXw7An1tsyvLTZrQhvqCDIAagv7s5NopKA="; // this is based on the collection data

            var handler = _server.CreateHandler();
            var client = ClientExtensions.CreateClient(handler);
            client.BaseAddress = _server.BaseAddress;
            client.DefaultRequestHeaders.Add(TestViewModelCollectionQueryProvider.HeaderName,
                new TimedEntityTagHeaderValue(ETagThatHashingGenerates).ETag.Tag);
            var response = await client.GetAsync("/api/withquery");
            var response2 = await client.GetAsync("/api/withquery");
            var serverCch = response2.GetCacheCowHeader();
            Assert.NotNull(serverCch);
            Assert.True(serverCch.ShortCircuited);

            var cch = response2.Headers.GetCacheCowHeader();
            Assert.NotNull(cch);
            Assert.False(cch.DidNotExist);
            Assert.True(cch.CacheValidationApplied);
            Assert.True(cch.RetrievedFromCache);
        }
    }

    public class TestViewModelQueryProvider : ITimedETagQueryProvider<TestViewModel>
    {
        public void Dispose()
        {
        }

        public Task<TimedEntityTagHeaderValue> QueryAsync(HttpContext context)
        {
            return null; // forces to use hasing
        }
    }

    public class TestViewModelCollectionQueryProvider : ITimedETagQueryProvider<IEnumerable<TestViewModel>>
    {
        
        public const string HeaderName = "x-test-etag";
        public void Dispose()
        {
        }

        public async Task<TimedEntityTagHeaderValue> QueryAsync(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey(HeaderName))
                return new TimedEntityTagHeaderValue(context.Request.Headers[HeaderName]);
            return null;
        }

        public TestViewModelCollectionQueryProvider()
        {

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
            services.AddQueryProviderForViewModel<TestViewModel, TestViewModelQueryProvider>();
            services.AddQueryProviderForViewModel<IEnumerable<TestViewModel>, TestViewModelCollectionQueryProvider>();
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
