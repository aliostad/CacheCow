using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;


namespace CacheCow.Server.Core.Mvc.Tests
{
    public class KestrelTests : ILoggerProvider, ILogger, IDisposable
    {
        private const int PORT_NUMBER = 12309;

        private IWebHost _host;
        private ITestOutputHelper _console;
        public List<Exception> _errors = new List<Exception>();

        public KestrelTests(ITestOutputHelper console)
        {
            _console = console;
            _host = WebHost.CreateDefaultBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Any, PORT_NUMBER);
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(this);
                })
                .UseStartup<WithCustomExtractorStartup>()
                .Build();
            _host.Start();
        }

        [Fact]
        public async Task DoesGetValidationAndReturns304_WithNoError_Issue241()
        {
            var relUrl = "/api/withquery";
            var client = new HttpClient();
            client.BaseAddress = new Uri($"http://localhost:{PORT_NUMBER}");
            var response = await client.GetAsync(relUrl);
            var lastMod = response.Content.Headers.LastModified.Value;
            var req = new HttpRequestMessage(HttpMethod.Get, relUrl);
            req.Headers.IfModifiedSince = lastMod;
            response = await client.SendAsync(req);
            if (response.Content != null)
            {
                var s = await response.Content.ReadAsStringAsync();
            }

            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
            Assert.Empty(_errors);
        }

        public void Dispose()
        {
            _host?.StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            _host?.Dispose();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return this;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (exception != null)
                _errors.Add(exception);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return (logLevel > LogLevel.Warning);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }


}
