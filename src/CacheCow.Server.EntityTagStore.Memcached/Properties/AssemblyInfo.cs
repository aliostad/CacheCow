using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("CacheCow.Server.EntityTagStore.Memcached")]
[assembly: AssemblyDescription("Memcached persistence for server-side CacheCow")]

#if BUILDTESTS
[assembly: InternalsVisibleTo("CacheCow.Server.EntityTagStore.Memcached.Tests")]
#endif