using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Client
{
	public interface ICacheMetadataProvider
	{
		IEnumerable<string> GetDomains();
		IEnumerable<CacheItemMetadata> GetItemsMetadata(string domain);
		CacheItemMetadata GetLastAccessedItem(string domain);
		CacheItemMetadata GetLastAccessedItem();
	}
}
