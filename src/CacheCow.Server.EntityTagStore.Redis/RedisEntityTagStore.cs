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

        public RedisEntityTagStore(string connectionString)
        {
            Init(ConnectionMultiplexer.Connect(connectionString));
        }

        public RedisEntityTagStore(ConnectionMultiplexer connection)
        {
            Init(connection);
        }

        public RedisEntityTagStore(IDatabase database)
        {
            _database = database;
        }

        private void Init(ConnectionMultiplexer connection)
        {
            _connection = connection;
            _database = _connection.GetDatabase();
        }

        public void Dispose()
        {
            if(_connection!=null)
                _connection.Dispose();
        }

        public TimeSpan? Expiry { get; set; }

        public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
        {
            eTag = null;
            string value = _database.StringGet(key.HashBase64);
            if (value != null)
            {
                return TimedEntityTagHeaderValue.TryParse(value, out eTag);
            }
            return false;
        }

        public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag)
        {
            _database.StringSet(key.HashBase64, eTag.ToString(), Expiry);
            _database.SetAdd("ResourceUri_" + key.ResourceUri, key.HashBase64);
            _database.SetAdd("RoutePattern_" + key.RoutePattern, key.HashBase64);
        }

        public int RemoveResource(string resourceUri)
        {
            int i = 0;
            string key = "ResourceUri_" + resourceUri;
            var redisValues = _database.SetScan(key);
            foreach (var value in redisValues)
            {
                TryRemove(value);
                i++;
            }
            TryRemove(key);
            return i;
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
            int i = 0;
            string key = "RoutePattern_" + routePattern;
            var redisValues = _database.SetScan(key);
            foreach (var value in redisValues)
            {
                i++;
                TryRemove(value);
            }
            TryRemove(key);
            return i;
        }

        public void Clear()
        {
            throw new NotSupportedException("Redis does not supprt Clear().");
        }
    }
}
