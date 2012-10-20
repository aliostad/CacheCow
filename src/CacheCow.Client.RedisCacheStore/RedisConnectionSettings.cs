using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Client.RedisCacheStore
{
	public class RedisConnectionSettings
	{
		public const int DefaultPort = 6379;

		public RedisConnectionSettings()
		{
			HostName = "localhost";
			Port = DefaultPort;
			IoTimeout = TimeSpan.Zero;
			AllowAdmin = false;
			SynTimeout = TimeSpan.FromSeconds(10);
			MaxUnsentBytes = 1 << 30;
			DatabaseId = 0;
		}

		public string HostName { get; set; }

		public string Password { get; set; }

		public int Port { get; set; }

		public TimeSpan IoTimeout { get; set; }

		public int MaxUnsentBytes { get; set; }

		public bool AllowAdmin { get; set; }

		public TimeSpan SynTimeout { get; set; }

		public int DatabaseId { get; set; }

		public static RedisConnectionSettings Parse(string connectionString)
		{
			throw new NotImplementedException();
		}

	}
}
