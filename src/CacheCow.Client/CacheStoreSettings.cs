using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Client
{
	public class CacheStoreSettings
	{

		public CacheStoreSettings()
		{
			TotalQuota = long.MaxValue;
			PerDomainQuota = 50*1024*1024; // 50 MB
		}

		/// <summary>
		/// Total number of bytes that can be used up by the cache.
		/// If 0 then no limit
		/// </summary>
		public long TotalQuota { get; set; }

		/// <summary>
		/// Quota per hostname (domain) in bytes. www.google.com will be a different host to google.com
		/// </summary>
		public long PerDomainQuota { get; set; }

	}
}
