using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Client
{
	public class CacheItemMetadata
	{
		public Byte[] Key { get; set; }
		public DateTime LastAccessed { get; set; }
		public long Size { get; set; }
		public string Domain { get; set; }
	}
}
