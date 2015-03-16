using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Common;
using StackExchange.Redis;

namespace CacheCow.Server.EntityTagStore.Redis
{
    public class RedisEntityTagStore : IEntityTagStore
    {
        private ConnectionMultiplexer _connection;
        private IDatabase _database;
        private TimeSpan? _expiry;

        private const string ResourceFormat = "ResourceUri:{0}";
        private const string RoutePatternFormat = "RoutePattern:{0}";

        public RedisEntityTagStore(string connectionString, 
            int databaseId = 0,
            TimeSpan? expiry = null)
        {
            _expiry = expiry;
            Init(ConnectionMultiplexer.Connect(connectionString), databaseId);
        }

        public RedisEntityTagStore(ConnectionMultiplexer connection, 
            int databaseId = 0,
            TimeSpan? expiry = null)
        {
            _expiry = expiry;
            Init(connection, databaseId);
        }

        public RedisEntityTagStore(IDatabase database, TimeSpan? expiry)
        {
            _database = database;
            _expiry = expiry;
        }

        private void Init(ConnectionMultiplexer connection, int databaseId = 0)
        {
            _connection = connection;
            _database = _connection.GetDatabase(databaseId);
        }

        public void Dispose()
        {
            if(_connection!=null)
                _connection.Dispose();
        }

        public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
        {
            eTag = null;
            string value = _database.StringGet(key.HashBase64);
            if (!string.IsNullOrEmpty(value))
            {
                return TimedEntityTagHeaderValue.TryParse(value, out eTag);
            }

            return false;
        }

        public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag)
        {
            _database.StringSet(key.HashBase64, eTag.ToString(), _expiry);
            
            // resource
            var resourceKey = string.Format(ResourceFormat, key.ResourceUri);
            _database.SetAdd(resourceKey, key.HashBase64);
            if (_expiry.HasValue)
                _database.KeyExpire(resourceKey, _expiry);

            // routePattern
            var routePatternKey = string.Format(RoutePatternFormat, key.RoutePattern);
            _database.SetAdd(routePatternKey, key.HashBase64);
            if (_expiry.HasValue)
                _database.KeyExpire(routePatternKey, _expiry);

        }

        public int RemoveResource(string resourceUri)
        {         
            string key = string.Format(ResourceFormat, resourceUri);
            var count = 0;
            foreach (var member in _database.SetMembers(key))
            {
                if (TryRemove(member))
                    count++;
            }

            return count;
        }

        public bool TryRemove(CacheKey key)
        {
            return TryRemove(key.HashBase64);
        }

        private bool TryRemove(string key)
        {
            return _database.KeyDelete(key);
        }

        public int RemoveAllByRoutePattern(string routePattern)
        {
            int count = 0;
            string key = string.Format(RoutePatternFormat, routePattern);
            foreach (var member in _database.SetMembers(key))
            {
                if (TryRemove(member))
                    count++;
            }

            return count;
        }

        public void Clear()
        {
            throw new NotSupportedException("StackExchange.Redis does not supprt Clear() but you can use FLUSH through redis-cli.");
        }
    }
}
