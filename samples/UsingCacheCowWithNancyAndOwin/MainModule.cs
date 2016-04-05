using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;

namespace UsingCacheCowWithNancyAndOwin
{
    public class MainModule : NancyModule
    {
        public MainModule()
        {
            Get["/"] = parameters =>
            {
                return "<h1>Welcome to My Photoblog!</h1><p>Nothing to see here at the moment.</p>";
            };
        }
    }
}