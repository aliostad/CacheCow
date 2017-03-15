using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Server.RoutePatternPolicy;
using NUnit.Framework;

namespace CacheCow.Tests.Server.RoutePatternPolicy
{
    [TestFixture]
    public class RestfulRoutePatternProviderTests
    {
        [TestCase("http://x/api/stuff/00000000-0000-0000-0000-000000000000/morestuff/00000000-0000-0000-0000-000000000000",
            new[]
            {
                "api/stuff/00000000-0000-0000-0000-000000000000/morestuff",
                "api/morestuff/00000000-0000-0000-0000-000000000000",
                "api/morestuff"
            })]
        [TestCase("http://x/api/stuff/00000000-0000-0000-0000-000000000000/morestuff/00000000-0000-0000-0000-000000000000/",
            new[]
            {
                "api/stuff/00000000-0000-0000-0000-000000000000/morestuff",
                "api/morestuff/00000000-0000-0000-0000-000000000000"
            })]
        [TestCase("http://x/api/stuff/00000000-0000-0000-0000-000000000000/morestuff/00000000-0000-0000-0000-000000000000/otherstuff/00000000-0000-0000-0000-000000000000",
            new[]
            {
                "api/stuff/00000000-0000-0000-0000-000000000000/morestuff/00000000-0000-0000-0000-000000000000/otherstuff",
                "api/otherstuff/00000000-0000-0000-0000-000000000000"
            })]
        [TestCase("http://x/api/morestuff/00000000-0000-0000-0000-000000000000",
            new[]
            {
                "api/morestuff"
            })]
        [TestCase("http://x/api/stuff00000000-0000-0000-0000-000000000000/morestuff/", new string[0])]
        public void GetLinkedRoutePatterns_Restful(string url, string[] exptectedPatterns)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var routePatternProvider = new RestfulRoutePatternProvider();

            var routePatterns = routePatternProvider.GetLinkedRoutePatterns(request);

            foreach (var exptectedPattern in exptectedPatterns)
            {
                Console.WriteLine(exptectedPattern);

                if (routePatterns.All(x => x != exptectedPattern))
                {
                    Console.WriteLine("These were returned:");
                    routePatterns.ToList().ForEach(x => Console.WriteLine("\t" + x));
                    Assert.Fail("could not find " + exptectedPattern);
                }
            }

        }
    }
}
