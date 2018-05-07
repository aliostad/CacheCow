using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using CacheCow.Client.Headers;
using CacheCow.Common.Helpers;

namespace CacheCow.Samples.Common
{
    public class ConsoleMenu
    {
        private readonly HttpClient _client;
        private bool _verbose = false;

        public ConsoleMenu(HttpClient client)
        {
            this._client = client;
        }

        public async Task Menu()
        {
            while (true)
            {
                Console.WriteLine(
@"CacheCow Cars Samples - (ASP.NET Core MVC and HttpClient)
    - Press 0 to list all cars
    - Press 1 to get the last item
    - Press 2 to create a new car and add to repo
    - Press 3 to update the last item (updates last modified)
    - Press 4 to delete the last item
    - Press 5 to delete the first item
    - Press 6 to toggle on/off verbose header dump
    - Press x to exit
"
);
                var key = Console.ReadKey(true);
                switch (key.KeyChar)
                {
                    case 'x':
                        return;
                    case '0':
                        await ListAll();
                        break;
                    case '2':
                        await CreateNew();
                        break;
                    case '3':
                        await UpdateLast();
                        break;
                    case '4':
                        await DeleteLast();
                        break;
                    case '1':
                        await GetLast();
                        break;
                    case '5':
                        await DeleteFirst();
                        break;
                    case '6':
                        Toggle();
                        break;
                    default:
                        // nothing
                        break;
                }
            }
        }

        public void Toggle()
        {
            _verbose = !_verbose;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(_verbose ? "Verbose toggle is ON" : "Verbose toggle is OFF");
            Console.WriteLine();
            Console.ResetColor();
        }

        public void DumpHeaders(HttpResponseMessage response)
        {
            if (!_verbose)
                return;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"REQUEST:\r\n{response.RequestMessage.Headers.ToString()}");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"RESPONSE:\r\n{response.Headers.ToString()}");
            if (response.Content != null)
            {
                Console.WriteLine($"RESPONSE CONTENT:\r\n{response.Content.Headers.ToString()}");
                Console.WriteLine();
            }
            Console.ResetColor();

        }

        public async Task ListAll()
        {
            var response = await _client.GetAsync("/api/cars");
            await response.WhatEnsureSuccessShouldHaveBeen();
            await response.Content.LoadIntoBufferAsync();
            WriteCacheCowHeader(response);
            Console.ForegroundColor = ConsoleColor.White;
            var cars = await response.Content. ReadAsAsync<IEnumerable<Car>>();

            Console.WriteLine("-----------------------------------------------------------------");
            Console.WriteLine($"| Id\t| NumberPlate\t| Year\t| Last Modified Date\t\t|");

            foreach (var c in cars)
            {
                Console.WriteLine($"| {c.Id}\t| {c.NumberPlate}\t| {c.Year}\t| {c.LastModified}\t|");
            }

            Console.WriteLine("-----------------------------------------------------------------");
            Console.ResetColor();

            DumpHeaders(response);
        }

        public async Task CreateNew()
        {
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/car"));
            await response.WhatEnsureSuccessShouldHaveBeen();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Location header: {response.Headers.Location}");
            Console.WriteLine();
            Console.ResetColor();

            DumpHeaders(response);
        }

        public async Task UpdateLast()
        {
            var id = InMemoryCarRepository.Instance.GetLastId();
            if (id.HasValue)
            {
                var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/car/{id.Value}"));
                await response.WhatEnsureSuccessShouldHaveBeen();
                DumpHeaders(response);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Repo is empty");
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        public async Task DeleteLast()
        {
            var id = InMemoryCarRepository.Instance.GetLastId();
            if (id.HasValue)
            {
                var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/car/{id.Value}"));
                await response.WhatEnsureSuccessShouldHaveBeen();
                DumpHeaders(response);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Repo is empty");
                Console.WriteLine();
                Console.ResetColor();
            }

        }

        public async Task DeleteFirst()
        {
            var id = InMemoryCarRepository.Instance.GetFirstId();
            if (id.HasValue)
            {
                var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/car/{id.Value}"));
                await response.WhatEnsureSuccessShouldHaveBeen();
                DumpHeaders(response);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Repo is empty");
                Console.WriteLine();
                Console.ResetColor();
            }

        }
        public async Task GetLast()
        {
            var id = InMemoryCarRepository.Instance.GetLastId();
            if (id.HasValue)
            {
                var response = await _client.GetAsync($"/api/car/{id.Value}");
                await response.WhatEnsureSuccessShouldHaveBeen();
                WriteCacheCowHeader(response);
                Console.ForegroundColor = ConsoleColor.White;
                var c = await response.Content.ReadAsAsync<Car>();
                Console.WriteLine($"| {c.Id}\t| {c.NumberPlate}\t| {c.Year}\t| {c.LastModified} |");
                Console.WriteLine();
                Console.ResetColor();
                DumpHeaders(response);
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
            if (response.Headers.Contains(CacheCow.Server.Headers.CacheCowHeader.Name))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Server: " +
                    response.Headers.GetValues(CacheCow.Server.Headers.CacheCowHeader.Name).FirstOrDefault() ?? "");
            }

            Console.ResetColor();
            Console.WriteLine();
        }

    }
}
