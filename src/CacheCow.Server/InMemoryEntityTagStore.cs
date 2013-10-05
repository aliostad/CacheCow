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

        private MemoryCache _eTagCache = new MemoryCache(ETagCacheName);
        private MemoryCache _routePatternCache = new MemoryCache(RoutePatternCacheName);


		public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
		{
		    eTag = (TimedEntityTagHeaderValue) _eTagCache.Get(key.HashBase64);
		    return eTag != null;
		}

		public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag)
		{
			_eTagCache.Set(key.HashBase64, eTag, DateTimeOffset.MaxValue);

		    var hashSet = new HashSet<CacheKey>();
                
            hashSet = (HashSet<CacheKey>) _routePatternCache.AddOrGetExisting(key.RoutePattern,hashSet
                , DateTimeOffset.MaxValue) ?? hashSet;

		    hashSet.Add(key);
		}

		public bool TryRemove(CacheKey key)
		{
		    return _eTagCache.Remove(key.HashBase64) != null;             
		}

		public int RemoveAllByRoutePattern(string routePattern)
		{
			int count = 0;
            var keys = (HashSet<CacheKey>)_routePatternCache.Get(routePattern);
            
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
		}

	    public void Dispose()
	    {
            _eTagCache.Dispose();
	        _routePatternCache.Dispose();
	    }
	}

}
