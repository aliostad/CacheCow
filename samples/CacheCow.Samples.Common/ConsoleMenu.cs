using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using CacheCow.Client.Headers;
using CacheCow.Common.Helpers;
using System.Net.Http.Headers;

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
    - Press A to list all cars
    - Press L to get the last item (default which is JSON)
    - Press X to get the last item in XML
    - Press C to create a new car and add to repo
    - Press U to update the last item (updates last modified)
    - Press O to update the last item outside API (updates last modified)
    - Press D to delete the last item
    - Press F to delete the first item
    - Press V to toggle on/off verbose header dump
    - Press Q to exit
"
);
                try
                {
                    var key = Console.ReadKey(true);
                    switch (key.KeyChar)
                    {
                        case 'q':
                        case 'Q':
                            return;
                        case 'a':
                        case 'A':
                            await ListAll();
                            break;
                        case 'C':
                        case 'c':
                            await CreateNew();
                            break;
                        case 'U':
                        case 'u':
                            await UpdateLast();
                            break;
                        case 'O':
                        case 'o':
                            await UpdateLastOutsideApi();
                            break;
                        case 'D':
                        case 'd':
                            await DeleteLast();
                            break;
                        case 'L':
                        case 'l':
                            await GetLast();
                            break;
                        case 'X':
                        case 'x':
                            await GetLastInXml();
                            break;
                        case 'F':
                        case 'f':
                            await DeleteFirst();
                            break;
                        case 'V':
                        case 'v':
                            Toggle();
                            break;
                        default:
                            // nothing
                            Console.WriteLine("Invalid option: " + key.KeyChar);
                            break;
                    }
                }
                catch(Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.WriteLine(e.ToString());
                    Console.ResetColor();
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
            WriteCacheCowHeader(response);
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
                WriteCacheCowHeader(response);
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

        public async Task UpdateLastOutsideApi()
        {
            var id = InMemoryCarRepository.Instance.GetLastId();
            if (id.HasValue)
            {
                InMemoryCarRepository.Instance.UpdateCar(id.Value);
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
                WriteCacheCowHeader(response);
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
                WriteCacheCowHeader(response);
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
        public async Task GetLastInXml()
        {
            var id = InMemoryCarRepository.Instance.GetLastId();
            if (id.HasValue)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/car/{id.Value}");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                var response = await _client.SendAsync(request);
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
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Status: {response.StatusCode}");
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
