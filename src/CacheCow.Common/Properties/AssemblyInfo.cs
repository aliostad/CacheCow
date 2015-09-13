using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("CacheCow.Common")]
[assembly: AssemblyDescription("Common library for CacheCow project")]

#if BUILDTESTS
[assembly: InternalsVisibleTo("CacheCow.Tests")]
#endif
