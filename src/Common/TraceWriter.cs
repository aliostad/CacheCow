using System;
using System.Diagnostics;

namespace CacheCow
{

	internal class TraceWriter
	{

		public const string CacheCowTraceSwitch = "CacheCow";

		private static readonly TraceSwitch _switch = new TraceSwitch(CacheCowTraceSwitch, "CacheCow Trace Switch");

		public static void WriteLine(string message, TraceLevel level, params object[] args)
		{
			if (args.Length > 0)
				message = string.Format(message, args);

			message = DateTime.Now.ToString() + " " + message;
			Trace.WriteLineIf(level >= _switch.Level, message);
		
		}

	}
}
