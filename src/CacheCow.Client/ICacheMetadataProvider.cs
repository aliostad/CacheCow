using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Client
{
	public interface ICacheMetadataProvider
	{
		IDictionary<string, long> GetDomainSizes();
		CacheItemMetadata GetEarliestAccessedItem(string domain);
		CacheItemMetadata GetEarliestAccessedItem();
	}
}
