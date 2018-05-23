using CacheCow.Client;
using CacheCow.Samples.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Threading.Tasks;

namespace CacheCow.Samples.Carter
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());
            var handler = server.CreateHandler();

            var client = ClientExtensions.CreateClient(handler);
            client.BaseAddress = server.BaseAddress;

            var p = new ConsoleMenu(client);

            Task.Run(async () => await p.Menu()).Wait();

        }
    }
}
