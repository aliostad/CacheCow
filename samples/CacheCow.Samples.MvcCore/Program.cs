using CacheCow.Client;
using CacheCow.Client.Headers;
using CacheCow.Samples.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System.Net.Http;
using System.Threading.Tasks;

namespace CacheCow.Samples.MvcCore
{

    class Program
    {
     
        static void Main(string[] args)
        {
            // setup
            var server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());
            var handler = server.CreateHandler();

            var client = ClientExtensions.CreateClient(handler);
            client.BaseAddress = server.BaseAddress;

            var p = new MenuBase(client);

            Task.Run(async () => await p.Menu()).Wait();

        }
    }
}
