using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                    await action().ConfigureAwait(false);
            };
        }

		public static string ToHex(this byte[] data)
		{
            StringBuilder hex = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

		public static byte[] FromHex(this string hex)
		{
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

	}

}
