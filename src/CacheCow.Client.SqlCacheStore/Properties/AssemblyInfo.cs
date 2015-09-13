using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("CacheCow.Client.SqlCacheStore")]
[assembly: AssemblyDescription("SQL Server storage for HTTP caching in CacheCow library")]

#if BUILDTESTS
[assembly: InternalsVisibleTo("CacheCow.Client.SqlCacheStore.Tests")]
#endif
