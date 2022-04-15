using System;
using System.Diagnostics;
using Xunit;

namespace CacheCow
{
    public class TraceSwitchTests
    {
        [Fact]
        public void IsThatGood()
        {
            Environment.SetEnvironmentVariable(TraceWriter.CacheCowTracingEnvVarName, "4");
            Assert.Equal(TraceLevel.Verbose, TraceWriter._switch.Level);
        }
    }
}
