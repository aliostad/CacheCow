using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Common;

namespace CacheCow.Common
{
	/// <summary>
	/// This is an interface representing an ETag store acting similar to a dictionary. 
	/// storing and retriving ETags.
	///  
	/// In a single-server scenario, this could be an in-memory disctionary implementation
	/// while in a server farm, this will be a persistent store.
	/// </summary>
	public interface IEntityTagStore : IDisposable
	{
		Task<TimedEntityTagHeaderValue> GetValueAsync(CacheKey key);
		Task AddOrUpdateAsync(CacheKey key, TimedEntityTagHeaderValue eTag);
        Task<int> RemoveResourceAsync(string resourceUri);
        Task<bool> TryRemoveAsync(CacheKey key);
		Task<int> RemoveAllByRoutePatternAsync(string routePattern);
		Task ClearAsync();
	}
}
