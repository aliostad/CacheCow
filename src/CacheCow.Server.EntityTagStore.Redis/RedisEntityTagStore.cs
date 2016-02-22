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

        public async Task<TimedEntityTagHeaderValue> GetValueAsync(CacheKey key)
        {
            string value = await _database.StringGetAsync(key.HashBase64);
            TimedEntityTagHeaderValue eTag = null;
            if (!string.IsNullOrEmpty(value))
            {
                TimedEntityTagHeaderValue.TryParse(value, out eTag);
            }

            return eTag;
        }

        public async Task AddOrUpdateAsync(CacheKey key, TimedEntityTagHeaderValue eTag)
        {
            await _database.StringSetAsync(key.HashBase64, eTag.ToString(), _expiry);

            // resource
            var resourceKey = string.Format(ResourceFormat, key.ResourceUri);
            await _database.SetAddAsync(resourceKey, key.HashBase64);
            if (_expiry.HasValue)
                await _database.KeyExpireAsync(resourceKey, _expiry);

            // routePattern
            var routePatternKey = string.Format(RoutePatternFormat, key.RoutePattern);
            await _database.SetAddAsync(routePatternKey, key.HashBase64);
            if (_expiry.HasValue)
                await _database.KeyExpireAsync(routePatternKey, _expiry);
        }

        public async Task<int> RemoveResourceAsync(string resourceUri)
        {
            string key = string.Format(ResourceFormat, resourceUri);
            var count = 0;
            foreach (var member in _database.SetMembers(key))
            {
                if (await TryRemoveAsync(member))
                    count++;
            }

            return count;
        }

        public Task<bool> TryRemoveAsync(CacheKey key)
        {
            return TryRemoveAsync(key.HashBase64);
        }

        private Task<bool> TryRemoveAsync(string key)
        {
            return _database.KeyDeleteAsync(key);
        }

        public async Task<int> RemoveAllByRoutePatternAsync(string routePattern)
        {
            int count = 0;
            string key = string.Format(RoutePatternFormat, routePattern);
            foreach (var member in _database.SetMembers(key))
            {
                if (await TryRemoveAsync(member))
                    count++;
            }

            return count;
        }

        public Task ClearAsync()
        {
            throw new NotSupportedException("StackExchange.Redis does not supprt Clear() but you can use FLUSH through redis-cli.");
        }
    }
}
