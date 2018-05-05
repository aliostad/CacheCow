using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheCow.Server.WebApi
{
    public class HttpCacheCreatedEventArgs : EventArgs
    {
        public HttpCacheCreatedEventArgs(HttpCacheAttribute instance)
        {
            FilterInstance = instance;
        }

        public HttpCacheAttribute FilterInstance { get; }
    }
}
