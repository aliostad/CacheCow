using CacheCow.Client;
using CacheCow.Samples.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace CacheCow.Samples.WebApi
{
    class Program
    {
        static void Main(string[] args)
        {
            const string BaseAddress = "http://localhost:18018";
            var config = new HttpSelfHostConfiguration(BaseAddress);

            config.Routes.MapHttpRoute(
                "API Collection", "api/{controller}s",
                new { id = RouteParameter.Optional });

            config.Routes.MapHttpRoute(
                "API Default", "api/{controller}/{id}",
                new { id = RouteParameter.Optional });

            var server = new HttpSelfHostServer(config);
            server.OpenAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var client = new HttpClient(
                new CachingHandler()
                {
                    InnerHandler = new HttpClientHandler()
                });

            client.BaseAddress = new Uri(BaseAddress);

            var menu = new ConsoleMenu(client);
            menu.Menu().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
