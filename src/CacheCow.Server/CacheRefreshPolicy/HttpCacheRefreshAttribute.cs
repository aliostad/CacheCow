using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Server.CacheRefreshPolicy
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HttpCacheRefreshAttribute : Attribute
    {

    }
}
