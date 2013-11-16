using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Routing;

namespace CacheCow.Server.RoutePatternPolicy
{
    public class ConventionalRoutePatternProvider
    {
        public static string CollectionPatternSign = "*";
        public static string InstancePatternSign = "+";

        private readonly HttpConfiguration _configuration;

        public ConventionalRoutePatternProvider(HttpConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetRoutePattern(HttpRequestMessage request)
        {
            var routeData = request.GetRouteData();
            if (routeData == null)
               return GetDefaultRoutePattern(request);

            var routeInfo = new RouteInfo(routeData.Route);

            // deal with no parameters
            if (routeInfo.Parameters.Count == 0)
                    return GetDefaultRoutePattern(request);


            // deal with catchall
            if (routeInfo.Parameters.Any(x => x.IsCatchAll))
                return GetDefaultRoutePattern(request);

            var lastParameter = routeInfo.Parameters.Last();
            if (routeData.Values[lastParameter.Name] == RouteParameter.Optional ||
                lastParameter.Name == "action")
            {
                return routeInfo.BuildCollectionPattern(request.RequestUri, routeData);
            }
            else
            {
                return routeInfo.BuildInstancePattern(request.RequestUri, routeData);                
            }
        }

        private string GetDefaultRoutePattern(HttpRequestMessage message)
        {
            return message.RequestUri.AbsolutePath;
        }


        public string GetLinkedRoutePattern(HttpRequestMessage message)
        {
            throw new NotImplementedException();
        }

    }

    internal class RouteInfo
    {
        private const string RouteParameterPattern = @"\{([\w\*]+)\}";
        private IHttpRoute _route;
        private List<RouteParameterInfo> _parameters = new List<RouteParameterInfo>();

        public RouteInfo(IHttpRoute route)
        {
            _route = route;
            var matches = Regex.Matches(_route.RouteTemplate, RouteParameterPattern);
            int i = 0;
            foreach (Match match in matches)
            {
                string name = match.Groups[1].Value;
                string realName = name.Trim('*');
                object defaultValue = route.Defaults.ContainsKey(realName)
                                          ? route.Defaults[realName]
                                          : null;

                _parameters.Add(new RouteParameterInfo(name,
                     i, defaultValue));

                i++;
            }


        }

        public string BuildCollectionPattern(Uri uri, IHttpRouteData routeData)
        {
            return BuildPattern(uri, routeData, ConventionalRoutePatternProvider.CollectionPatternSign);
        }

        public string BuildInstancePattern(Uri uri, IHttpRouteData routeData)
        {
            return BuildPattern(uri, routeData, ConventionalRoutePatternProvider.InstancePatternSign);
        }

        private string BuildPattern(Uri uri, IHttpRouteData routeData, string sign)
        {
            var last = _parameters.Last();
            var routePattern = "/" + _route.RouteTemplate;
            foreach (var parameter in Parameters)
            {
                if (parameter == last)
                {
                    routePattern = routePattern.Replace("{" + parameter.Name + "}", sign);
                }
                else
                {
                    routePattern = routePattern.Replace("{" + parameter.Name + "}", routeData.Values[parameter.Name].ToString());
                }
            }

            return routePattern;
        }

        public ICollection<RouteParameterInfo> Parameters
        {
            get { return _parameters; }
        }
    }

    internal class RouteParameterInfo
    {

        public RouteParameterInfo(string name, int index, object defaultValue)
        {
            Name = name;
            Index = index;
            DefaultValue = defaultValue;
        }

        public int Index { get; private set; }

        public string Name { get; private set; }

        public object DefaultValue { get; private set; }

        public bool IsCatchAll
        {
            get { return Name.StartsWith("*"); }
        }

        public bool IsAction
        {
            get { return Name == "action"; }
        }

        public bool IsController
        {
            get { return Name == "action"; }
        }

        public bool IsOptional
        {
            get { return DefaultValue == RouteParameter.Optional; }
        }

    }

}
