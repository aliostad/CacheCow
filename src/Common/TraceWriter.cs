using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CacheCow
{
	internal static class TraceWriter
    {
        static TraceWriter()
        {
            ExamineEnvVar();
        }

		public const string CacheCowTraceSwitch = "CacheCow";
        public const string CacheCowTracingEnvVarName = "CacheCow.Tracing.Switch";

        // ALERT!! THIS NO LONGER WORKS https://github.com/dotnet/runtime/issues/67991
		internal static readonly TraceSwitch _switch = new TraceSwitch(CacheCowTraceSwitch, "CacheCow Trace Switch");

        private static void ExamineEnvVar()
        {
            var envvarValue = Environment.GetEnvironmentVariable(CacheCowTracingEnvVarName) ?? "";
            if (envvarValue.Length > 0)
            {
                TraceLevel level;
                if (Enum.TryParse(envvarValue, out level))
                    _switch.Level = level;
            }
        }

        public static void WriteLine(string message, TraceLevel level, params object[] args)
        {

            if (_switch.Level < level)
				return;

			string dateTimeOfEvent = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff");
			string callingMethod = string.Empty;
			try
			{
				callingMethod = new StackFrame(1).GetMethod().Name;
			}
			catch
			{
				// swallow
			}


			Trace.WriteLine(string.Format("{0} - {1}: {2}",
				dateTimeOfEvent,
				callingMethod,
				args.Length == 0 ? message : string.Format(message, args)
				));

		}
	}


}
