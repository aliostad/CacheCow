namespace CacheCow.Server.EntityTagStore.AzureCaching
{
	using System.Collections.Generic;
	using System.Linq;

	using CacheCow.Common;

	using Microsoft.ApplicationServer.Caching;

	/// <summary>
	/// The azure caching entity tag store.
	/// </summary>
	public class AzureCachingEntityTagStore : IEntityTagStore
	{
		private readonly DataCache cache;
		private const string CacheRegion = "CacheCowServer";

		public AzureCachingEntityTagStore()
		{
			cache = new DataCache();
			cache.CreateRegion(CacheRegion);
		}

		public void Dispose()
		{
			// nothing
		}

		public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
		{
			var cacheObject = cache.Get(key.HashBase64, CacheRegion) as string;

			if (!TimedEntityTagHeaderValue.TryParse(cacheObject, out eTag))
				return false;

			return true;
		}

		public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag)
		{
			cache.Put(key.HashBase64, eTag.ToString(), new[]
				                               {
					                               new DataCacheTag(key.ResourceUri),
												   new DataCacheTag(key.RoutePattern),
				                               }, CacheRegion);
		}

		public int RemoveResource(string resourceUri)
		{
			List<KeyValuePair<string, object>> cacheObjects = cache.GetObjectsByTag(new DataCacheTag(resourceUri), CacheRegion).ToList();

			if (!cacheObjects.Any())
			{
				return 0;
			}


			int rowsCount = 0;

			foreach (KeyValuePair<string, object> cacheObject in cacheObjects)
			{
				if (cacheObject.Value == null)
				{
					continue;
				}

				cache.Remove(cacheObject.Key, CacheRegion);
				rowsCount++;
			}

			return rowsCount;
		}

		public bool TryRemove(CacheKey key)
		{
			return cache.Remove(key.HashBase64);
		}

		public int RemoveAllByRoutePattern(string routePattern)
		{
			List<KeyValuePair<string, object>> cacheObjects = cache.GetObjectsByTag(new DataCacheTag(routePattern), CacheRegion).ToList();

			if (!cacheObjects.Any())
			{
				return 0;
			}

			int rowsCount = 0;

			foreach (KeyValuePair<string, object> cacheObject in cacheObjects)
			{
				if (cacheObject.Value == null)
				{
					continue;
				}

				cache.Remove(cacheObject.Key, CacheRegion);
				rowsCount++;
			}

			return rowsCount;
		}

		public void Clear()
		{
			cache.ClearRegion(CacheRegion);
		}
	}
}