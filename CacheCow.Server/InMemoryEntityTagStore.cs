using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CacheCow.Common;

namespace CacheCow.Server
{
	public class InMemoryEntityTagStore : IEntityTagStore
	{
		private readonly ConcurrentDictionary<CacheKey, TimedEntityTagHeaderValue> _eTagCache = new ConcurrentDictionary<CacheKey, TimedEntityTagHeaderValue>();
		private readonly ConcurrentDictionary<string, HashSet<CacheKey>> _routePatternCache = new ConcurrentDictionary<string, HashSet<CacheKey>>();

		public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
		{
			return _eTagCache.TryGetValue(key, out eTag);
		}

		public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag)
		{
			_eTagCache.AddOrUpdate(key, eTag, (theKey, oldValue) => eTag);
			_routePatternCache.AddOrUpdate(key.RoutePattern, new HashSet<CacheKey>() { key },
				(routePattern, hashSet) =>
				{
					hashSet.Add(key);
					return hashSet;
				});
		}

		public bool TryRemove(CacheKey key)
		{
			TimedEntityTagHeaderValue entityTagHeaderValue;
			return _eTagCache.TryRemove(key, out entityTagHeaderValue);
		}

		public int RemoveAllByRoutePattern(string routePattern)
		{
			int count = 0;
			HashSet<CacheKey> keys;
			if (_routePatternCache.TryGetValue(routePattern, out keys))
			{
				count = keys.Count;
				foreach (var entityTagKey in keys)
					this.TryRemove(entityTagKey);
				_routePatternCache.TryRemove(routePattern, out keys);
			}
			return count;
		}

		public void Clear()
		{
			_eTagCache.Clear();
		}
	}

}
