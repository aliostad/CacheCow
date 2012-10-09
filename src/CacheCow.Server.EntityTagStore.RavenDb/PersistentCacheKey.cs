using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Server.EntityTagStore.RavenDb
{
	public class PersistentCacheKey
	{
		public string Id { get; set; }
		public byte[] Hash { get; set; }
		public string RoutePattern { get; set; }
		public string ETag { get; set; }
		public DateTimeOffset LastModified { get; set; }
	}
}
