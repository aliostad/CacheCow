using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading.Tasks;

namespace CacheCow.Common.Helpers
{
	public static class BasicExtensions
	{
		public static bool IsIn<T>(this T item, params T[] list)
		{
			if (list == null || list.Length == 0)
				return false;
			return list.Any(x => EqualityComparer<T>.Default.Equals(x, item));
		}

		public static Action Chain(this IEnumerable<Action> actions)
		{
			return () =>
			{
				foreach (var action in actions)
					action();
			};
		}

        public static Func<Task> Chain(this IEnumerable<Func<Task>> actions)
        {
            return async () =>
            {
                foreach (var action in actions)
                    await action();
            };
        }

		public static string ToHex(this byte[] data)
		{
			var shb = new SoapHexBinary(data);
			return shb.ToString();
		}

		public static byte[] FromHex(this string data)
		{
			var shb = SoapHexBinary.Parse(data);
			return shb.Value;
		}

	}

}
