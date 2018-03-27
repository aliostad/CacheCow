using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Client.RedisCacheStore.Helper
{
	internal static class Extensions
	{
		public static string ToBase64(this byte[] bytes)
		{
			return Convert.ToBase64String(bytes);
		}
	}
}
