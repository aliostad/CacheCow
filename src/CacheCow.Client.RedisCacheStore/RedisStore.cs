using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Client.RedisCacheStore.Helper;
using CacheCow.Common;
using CacheCow.Common.Helpers;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace CacheCow.Client.RedisCacheStore
{
    /// <summary>
    /// Re-writing and removing the interface ICacheMetadataProvider as it is not really being used
    /// </summary>
	public class RedisStore : ICacheStore
	{
        private ConnectionMultiplexer _connection;
        private IDatabase _database;
		private bool _dispose;
		private MessageContentHttpMessageSerializer _serializer = new MessageContentHttpMessageSerializer();

         public RedisStore(string connectionString, 
            int databaseId = 0)
        {
            Init(ConnectionMultiplexer.Connect(connectionString), databaseId);
        }

        public RedisStore(ConnectionMultiplexer connection, 
            int databaseId = 0)
        {
            Init(connection, databaseId);
        }

        public RedisStore(IDatabase database)
        {
            _database = database;
        }

        private void Init(ConnectionMultiplexer connection, int databaseId = 0)
        {
            _connection = connection;
            _database = _connection.GetDatabase(databaseId);
        }

		public void Dispose()
		{
			if (_connection != null && _dispose)
				_connection.Dispose();
		}

        public async Task<HttpResponseMessage> GetValueAsync(CacheKey key)
        {
            HttpResponseMessage result = null;
            string entryKey = key.Hash.ToBase64();

            if (! await _database.KeyExistsAsync(entryKey))
                return null;

            byte[] value = await _database.StringGetAsync(entryKey);

            var memoryStream = new MemoryStream(value);
            return await _serializer.DeserializeToResponseAsync(memoryStream);
        }

        public async Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
        {
            var memoryStream = new MemoryStream();
            await _serializer.SerializeAsync(response, memoryStream);
            memoryStream.Position = 0;
            var data = memoryStream.ToArray();
            await _database.StringSetAsync(key.HashBase64, data);
        }

        public Task<bool> TryRemoveAsync(CacheKey key)
        {
            return _database.KeyDeleteAsync(key.HashBase64);
        }

        public Task ClearAsync()
        {
            throw new NotSupportedException("Currently not supported by StackExchange.Redis. Use redis-cli.exe"); 
        }
	}
}
