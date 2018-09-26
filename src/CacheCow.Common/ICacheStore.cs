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


		/// <summary>
		/// Gets the cached HTTP-Response from this storage
		/// </summary>
		/// <returns>The ResponseMessage if the key was found; null otherwise</returns>
		Task<HttpResponseMessage> GetValueAsync(CacheKey key);

		/// <summary>
		/// Adds the given response to the chachestore. If the key is already present, the old value is overwrittn
		/// </summary>
		Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response);

	    /// <summary>
	    /// (Tries to) remove the cached response correspondig with this key from the cache.
	    /// </summary>
	    /// <param name="key"></param>
	    /// <returns>True if deletion was successfull, False if not (e.g. the key was not in the store to begin with)</returns>
		Task<bool> TryRemoveAsync(CacheKey key);

	    /// <summary>
	    /// Nuke the cache
	    /// </summary>
		Task ClearAsync();

	}
}
