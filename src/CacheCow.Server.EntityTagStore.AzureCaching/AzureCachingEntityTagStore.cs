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
		private readonly DataCache _cache;
	    private string _regionName;
	    private const string DefaultCacheRegion = "CacheCowServer";



		public AzureCachingEntityTagStore()
            : this("default")
		{
			
		}

        public AzureCachingEntityTagStore(string cacheName)
            : this(cacheName, DefaultCacheRegion)
        {
           
        }

        public AzureCachingEntityTagStore(string cacheName, string regionName)
        {
            _regionName = regionName;
            _cache = new DataCache(cacheName);
            _cache.CreateRegion(regionName);
        }

        public AzureCachingEntityTagStore(DataCache cache)
            : this(cache, DefaultCacheRegion)
        {
          
        }

        public AzureCachingEntityTagStore(DataCache cache, string regionName)
        {
            _regionName = regionName;
            _cache = cache;
            _cache.CreateRegion(regionName);
        }

		public void Dispose()
		{
			// nothing
		}

		public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
		{
            var cacheObject = _cache.Get(key.HashBase64, _regionName) as string;

			if (!TimedEntityTagHeaderValue.TryParse(cacheObject, out eTag))
				return false;

			return true;
		}

		public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag)
		{
			_cache.Put(key.HashBase64, eTag.ToString(), new[]
				                               {
					                               new DataCacheTag(key.ResourceUri),
												   new DataCacheTag(key.RoutePattern),
				                               }, _regionName);
		}

		public int RemoveResource(string resourceUri)
		{
            List<KeyValuePair<string, object>> cacheObjects = _cache.GetObjectsByTag(new DataCacheTag(resourceUri), _regionName).ToList();

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

                _cache.Remove(cacheObject.Key, _regionName);
				rowsCount++;
			}

			return rowsCount;
		}

		public bool TryRemove(CacheKey key)
		{
			return _cache.Remove(key.HashBase64);
		}

		public int RemoveAllByRoutePattern(string routePattern)
		{
            List<KeyValuePair<string, object>> cacheObjects = _cache.GetObjectsByTag(new DataCacheTag(routePattern), _regionName).ToList();

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

                _cache.Remove(cacheObject.Key, _regionName);
				rowsCount++;
			}

			return rowsCount;
		}

		public void Clear()
		{
            _cache.ClearRegion(_regionName);
		}
	}
}