using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("CacheCow.Client.MemcachedCacheStore")]
[assembly: AssemblyDescription("Memcached storage for HTTP caching in CacheCow library")]

#if BUILDTESTS
[assembly: InternalsVisibleTo("CacheCow.Client.MemcachedCacheStore.Tests")]
#endif