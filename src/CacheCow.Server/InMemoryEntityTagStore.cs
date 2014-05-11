using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using CacheCow.Common;

namespace CacheCow.Server
{
	public class InMemoryEntityTagStore : IEntityTagStore
	{

        private const string ETagCacheName = "###_InMemoryEntityTagStore_ETag_###";
        private const string RoutePatternCacheName = "###_InMemoryEntityTagStore_RoutePattern_###";
        private const string ResourceCacheName = "###_InMemoryEntityTagStore_Resource_###";

        private MemoryCache _eTagCache = new MemoryCache(ETagCacheName);
        private MemoryCache _routePatternCache = new MemoryCache(RoutePatternCacheName);
        private MemoryCache _resourceCache = new MemoryCache(ResourceCacheName);


		public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
		{
		    eTag = (TimedEntityTagHeaderValue) _eTagCache.Get(key.HashBase64);
		    return eTag != null;
		}

		public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag)
		{
			_eTagCache.Set(key.HashBase64, eTag, DateTimeOffset.MaxValue);

            // route pattern
		    var bag = new ConcurrentBag<CacheKey>();
            bag = (ConcurrentBag<CacheKey>)_routePatternCache.AddOrGetExisting(key.RoutePattern, bag
                , DateTimeOffset.MaxValue) ?? bag;
		    bag.Add(key);

            // resource
            var rbag = new ConcurrentBag<CacheKey>();
            rbag = (ConcurrentBag<CacheKey>)_resourceCache.AddOrGetExisting(key.ResourceUri, rbag
                , DateTimeOffset.MaxValue) ?? rbag;
            rbag.Add(key);

		}

        public bool TryRemove(CacheKey key)
        {
            return _eTagCache.Remove(key.HashBase64) != null;
        }

	    public int RemoveResource(string resourceUri)
	    {
            int count = 0;
            var keys = (ConcurrentBag<CacheKey>)_resourceCache.Get(resourceUri);

            if (keys != null)
            {
                count = keys.Count;
                foreach (var entityTagKey in keys)
                    this.TryRemove(entityTagKey);
                _resourceCache.Remove(resourceUri);
            }

            return count;
	    }


		public int RemoveAllByRoutePattern(string routePattern)
		{
			int count = 0;
            var keys = (ConcurrentBag<CacheKey>)_routePatternCache.Get(routePattern);
            
            if (keys != null)
            {
				count = keys.Count;
                foreach (var entityTagKey in keys)
					this.TryRemove(entityTagKey);
                _routePatternCache.Remove(routePattern);
            }
			
			return count;
		}

		public void Clear()
		{
            _eTagCache.Dispose();
            _eTagCache = new MemoryCache(ETagCacheName);

            _routePatternCache.Dispose();
            _routePatternCache = new MemoryCache(RoutePatternCacheName);

            _resourceCache.Dispose();
            _resourceCache = new MemoryCache(ResourceCacheName);
		}

	    public void Dispose()
	    {
            _eTagCache.Dispose();
	        _routePatternCache.Dispose();
            _resourceCache.Dispose();
	    }
	}

}
