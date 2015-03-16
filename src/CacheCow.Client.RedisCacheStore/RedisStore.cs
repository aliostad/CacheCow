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
		

		/// <summary>
		/// Gets the value if exists
		/// ------------------------------------------
		/// 
		/// Steps:
		/// 
		/// 1) Get the value
		/// 2) Update domain-based earliest access
		/// 3) Update global earliest access
		/// </summary>
		/// <param name="key"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public bool TryGetValue(CacheKey key, out HttpResponseMessage response)
		{
			HttpResponseMessage result = null;
			response = null;
			string entryKey = key.Hash.ToBase64();
            
			if (!_database.KeyExists(entryKey))
				return false;

			byte[] value = _database.StringGet(entryKey);
			
			var memoryStream = new MemoryStream(value);
            response = Task.Factory.StartNew(() => _serializer.DeserializeToResponseAsync(memoryStream).Result).Result; // offloading
		    return true;
		}

		public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
			var memoryStream = new MemoryStream();
		    Task.Factory.StartNew(() => _serializer.SerializeAsync(response.ToTask(), memoryStream).Wait()).Wait(); // offloading
		    memoryStream.Position = 0;
		    var data = memoryStream.ToArray();
		    _database.StringSet(key.HashBase64, data);
		}

        public bool TryRemove(CacheKey key)
        {
            return _database.KeyDelete(key.HashBase64);
        }

        public void Clear()
		{
			throw new NotSupportedException("Currently not supported by StackExchange.Redis. Use redis-cli.exe"); 
		}

		public void Dispose()
		{
			if (_connection != null && _dispose)
				_connection.Dispose();
		}
	}
}
