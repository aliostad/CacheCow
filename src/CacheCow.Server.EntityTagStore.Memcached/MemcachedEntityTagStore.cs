using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CacheCow.Common;
using Enyim.Caching;
using Enyim.Caching.Configuration;

namespace CacheCow.Server.EntityTagStore.Memcached
{
    public class MemcachedEntityTagStore : IEntityTagStore, IDisposable
    {

        private readonly MemcachedClient _memcachedClient;

        public MemcachedEntityTagStore()
        {
            _memcachedClient = new MemcachedClient();
        }

        public MemcachedEntityTagStore(IMemcachedClientConfiguration configuration)
        {
            _memcachedClient = new MemcachedClient(configuration);
        }

        public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
        {
            //_memcachedClient.ExecuteAppend("",new ArraySegment<byte>())
            throw new NotImplementedException();

        }

        public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag)
        {
            throw new NotImplementedException();
        }

        public bool TryRemove(CacheKey key)
        {
            throw new NotImplementedException();
        }

        public int RemoveAllByRoutePattern(string routePattern)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _memcachedClient.Dispose();
        }
    }
}
