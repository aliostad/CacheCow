using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Routing;

namespace CacheCow.Server.RoutePatternPolicy
{
    /// <summary>
    /// This class provides routePatterns for flat and conventional collection/instance ASP.NET routes.
    /// If you have hierarchies which get invalidated, you need to invalidate yourself.
    /// Rules for hierarchical can get very complex and hairy and is not possible to get right with conventional routing.
    /// </summary>
    public class ConventionalRoutePatternProvider : IRoutePatternProvider
    {
        public static string CollectionPatternSign = "*";
        public static string InstancePatternSign = "+";

        private readonly HttpConfiguration _configuration;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration">configuration</param>
        public ConventionalRoutePatternProvider(HttpConfiguration configuration)
        {
            _configuration = configuration;
        }


        /// <summary>
        /// This method must be used inside CacheKeyGenerator
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual string GetRoutePattern(HttpRequestMessage request)
        {
            var routeData = request.GetRouteData();
            if (routeData == null)
                routeData = _configuration.Routes.GetRouteData(request);

            if(routeData == null)
                return GetDefaultRoutePattern(request);

            var routeInfo = new RouteInfo(routeData.Route);

            // deal with no parameters
            if (routeInfo.Parameters.Count == 0)
                    return GetDefaultRoutePattern(request);


            // deal with catchall
            if (routeInfo.Parameters.Any(x => x.IsCatchAll))
                return GetDefaultRoutePattern(request);
         
            if (routeInfo.IsCollection(routeData))
            {
                return routeInfo.BuildCollectionPattern(request.RequestUri, routeData);
            }
            else
            {
                return routeInfo.BuildInstancePattern(request.RequestUri, routeData);                
            }
        }

        protected virtual string GetDefaultRoutePattern(HttpRequestMessage message)
        {
            return message.RequestUri.AbsolutePath;
        }


        /// <summary>
        /// This method must be set to LinkedRoutePatternProvider of CachingHandler
        /// </summary>
        /// <param name="message"></param>
        /// <returns>All linked route patterns</returns>
        public virtual IEnumerable<string> GetLinkedRoutePatterns(HttpRequestMessage request)
        {
            var routeData = request.GetRouteData();
            if (routeData == null)
                return new string[0];

            var routeInfo = new RouteInfo(routeData.Route);

            // deal with no parameters
            if (routeInfo.Parameters.Count == 0)
                return new string[0];

            // deal with catchall
            if (routeInfo.Parameters.Any(x => x.IsCatchAll))
                return new string[0];

            var linkedRoutePatterns = new List<string>();

            if (!routeInfo.IsCollection(routeData))
            {
                linkedRoutePatterns.Add(routeInfo.BuildCollectionPattern(request.RequestUri, routeData));
            }

            return linkedRoutePatterns;

        }


    }

    public class RouteInfo
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

        public bool IsCollection(IHttpRouteData routeData)
        {

            // if last parameter has optional value or is action then it is a collection 
            var lastParameter = Parameters.Last();
            return !routeData.Values.ContainsKey(lastParameter.Name) ||
                routeData.Values[lastParameter.Name] == RouteParameter.Optional ||
                    lastParameter.IsAction;

        }

        public string BuildCollectionPattern(Uri uri, IHttpRouteData routeData)
        {
            return BuildPattern(uri, routeData, true);
        }

        private string BuildPattern(Uri uri, IHttpRouteData routeData, bool isCollection)
        {
            var last = _parameters.Last();
            var routePattern = "/" + _route.RouteTemplate;
            foreach (var parameter in Parameters)
            {
                if (parameter == last)
                {
                    routePattern = routePattern.Replace("{" + parameter.Name + "}",
                        isCollection ? 
                            ConventionalRoutePatternProvider.CollectionPatternSign : 
                            ConventionalRoutePatternProvider.InstancePatternSign );
                }
                else
                {
                    routePattern = routePattern.Replace("{" + parameter.Name + "}",
                        routeData.Values[parameter.Name].ToString());
                }
            }

            return routePattern;
        }


        public string BuildInstancePattern(Uri uri, IHttpRouteData routeData)
        {
            return BuildPattern(uri, routeData, false);
        }


        public ICollection<RouteParameterInfo> Parameters
        {
            get { return _parameters; }
        }
    }

    public class RouteParameterInfo
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
            get { return Name == "controller"; }
        }

        public bool IsOptional
        {
            get { return DefaultValue == RouteParameter.Optional; }
        }

    }

}
