using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CacheCow.Common;

namespace CacheCow.Server.EntityTagStore.Sqlite
{
    public class SqliteEntityTagStore : IEntityTagStore
    {
        public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
        {
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
            throw new NotImplementedException();
        }
		
    }
}
