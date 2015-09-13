using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("CacheCow.Client")]
[assembly: AssemblyDescription("Client library for CacheCow project")]

#if BUILDTESTS
[assembly: InternalsVisibleTo("CacheCow.Client.Tests")]
#endif
