using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CacheCow.Common
{
	public interface ICacheStore : IDisposable
	{

		Task<HttpResponseMessage> GetValueAsync(CacheKey key);
		Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response);
		Task<bool> TryRemoveAsync(CacheKey key);
		Task ClearAsync();

	}
}
