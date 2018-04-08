using CacheCow.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CacheCow.Samples.MvcCore
{
    class Program
    {
        private static TestServer _server;
        private static HttpClient _client;

        static void Main(string[] args)
        {
            // setup
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());
            var handler = _server.CreateHandler();
            _client = ClientExtensions.CreateClient(handler);
            _client.BaseAddress = _server.BaseAddress;

            Task.Run(RunAsync).Wait();

        }

        static async Task RunAsync()
        {
            var result = await _client.GetAsync("/api/car/");
            Console.WriteLine(result.Headers);
            Console.WriteLine((int) result.StatusCode);
        }
    }
}
