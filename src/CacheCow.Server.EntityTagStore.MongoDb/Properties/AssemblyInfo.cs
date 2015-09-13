using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("CacheCow.Server.EntityTagStore.MongoDb")]
[assembly: AssemblyDescription("MongoDB persistence for server-side CacheCow")]

#if BUILDTESTS
[assembly: InternalsVisibleTo("CacheCow.Tests")]
#endif