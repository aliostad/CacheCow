using CacheCow.Client;
using CacheCow.Client.Headers;
using CacheCow.Samples.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CacheCow.Samples.MvcCore
{

    class Program : MenuBase
    {
        private TestServer _server;
        private HttpClient _client;

        static void Main(string[] args)
        {
            var p = new Program();
            // setup
            p._server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());
            var handler = p._server.CreateHandler();

            p._client = ClientExtensions.CreateClient(handler);
            p._client.BaseAddress = p._server.BaseAddress;

            Task.Run(async () => await p.Menu()).Wait();

        }

        

        public override async Task ListAll()
        {
            var response = await _client.GetAsync("/api/cars");
            response.EnsureSuccessStatusCode();
            await response.Content.LoadIntoBufferAsync();
            WriteCacheCowHeader(response);
            Console.ForegroundColor = ConsoleColor.White;
            var cars = await response.Content.ReadAsAsync<IEnumerable<Car>>();

            Console.WriteLine("-----------------------------------------------------------------");
            Console.WriteLine($"| Id\t| NumberPlate\t| Year\t| Last Modified Date\t\t|");

            foreach (var c in cars)
            {
                Console.WriteLine($"| {c.Id}\t| {c.NumberPlate}\t| {c.Year}\t| {c.LastModified}\t|");
            }

            Console.WriteLine("-----------------------------------------------------------------");
            Console.ResetColor();
        }

        public override async Task CreateNew()
        {
            var response = await _client.SendAsync( new HttpRequestMessage(HttpMethod.Post, "/api/car"));
            response.EnsureSuccessStatusCode();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Location header: {response.Headers.Location}");
            Console.WriteLine();
            Console.ResetColor();

        }

        public override async Task UpdateLast()
        {
            var id = InMemoryCarRepository.Instance.GetLastId();
            if(id.HasValue)
            {
                var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/car/{id.Value}"));
                response.EnsureSuccessStatusCode();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Repo is empty");
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        public override async Task DeleteLast()
        {
            var id = InMemoryCarRepository.Instance.GetLastId();
            if (id.HasValue)
            {
                var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/car/{id.Value}"));
                response.EnsureSuccessStatusCode();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Repo is empty");
                Console.WriteLine();
                Console.ResetColor();
            }

        }
        public override async Task GetLast()
        {
            var id = InMemoryCarRepository.Instance.GetLastId();
            if (id.HasValue)
            {
                var response = await _client.GetAsync($"/api/car/{id.Value}");
                response.EnsureSuccessStatusCode();
                WriteCacheCowHeader(response);
                Console.ForegroundColor = ConsoleColor.White;
                var c = await response.Content.ReadAsAsync<Car>();
                Console.WriteLine($"| {c.Id}\t| {c.NumberPlate}\t| {c.Year}\t| {c.LastModified} |");
                Console.WriteLine();
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Repo is empty");
                Console.WriteLine();
                Console.ResetColor();
            }

        }

        static void WriteCacheCowHeader(HttpResponseMessage response)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Client: {response.Headers.GetCacheCowHeader()}");
            if(response.Headers.Contains(CacheCowHeader.Name))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Server: " + 
                    response.Headers.GetValues(CacheCowHeader.Name).FirstOrDefault() ?? "");
            }

            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
