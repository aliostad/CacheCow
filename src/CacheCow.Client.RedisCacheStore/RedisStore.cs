using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Diagnostics;
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
	    private int _timeoutMilli;
	    private bool _throwExceptions;

	    public RedisStore(string connectionString, 
            int databaseId = 0,
            int timeoutMilli = 10000,
            bool throwExceptions = true)
	    {
	        _throwExceptions = throwExceptions;
	        _timeoutMilli = timeoutMilli;
	        try
	        {
                Init(ConnectionMultiplexer.Connect(connectionString), databaseId);
            }
	        catch (Exception e)
	        {
                if(_throwExceptions)
	                throw;
                else
                    Trace.WriteLine(e.ToString());
	        }
	        
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

        public Task<HttpResponseMessage> GetValueAsync(CacheKey key)
        {
            return FinishInTimeOrDie(DoGetValueAsync(key));
        }

	    private async Task<HttpResponseMessage> DoGetValueAsync(CacheKey key)
	    {
            HttpResponseMessage result = null;
            string entryKey = key.Hash.ToBase64();

            Trace.WriteLine("After hash");

            if (!await _database.KeyExistsAsync(entryKey))
                return null;

            Trace.WriteLine("After exists");

            byte[] value = await _database.StringGetAsync(entryKey);

            Trace.WriteLine("After get");

            var memoryStream = new MemoryStream(value);
            var r = await _serializer.DeserializeToResponseAsync(memoryStream);

            Trace.WriteLine("After deser");

            return r;
        }

        public Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
        {
            return FinishInTimeOrDie(DoAddOrUpdateAsync(key, response));
        }

	    public async Task<T> FinishInTimeOrDie<T>(Task<T> t1)
	    {
            var t2 = Task.Delay(_timeoutMilli);
            var index = Task.WaitAny(new[] { t1, t2 });
            try
            {
                if (index == 1)
                    return default(T);

                if (t1.IsFaulted)
                    throw t1.Exception;

                return t1.Result;
            }
            catch (Exception e)
            {
                if (_throwExceptions)
                    throw;
                else
                    Trace.WriteLine(e.ToString());

                return default(T);
            }
        }

	    private async Task<bool> DoAddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
	    {
            var memoryStream = new MemoryStream();
            await _serializer.SerializeAsync(response, memoryStream);
            memoryStream.Position = 0;
            var data = memoryStream.ToArray();
            var expiry = response.GetExpiry() ?? DateTimeOffset.UtcNow.AddDays(1);
            await _database.StringSetAsync(key.HashBase64, data, expiry.Subtract(DateTimeOffset.UtcNow));
            return true;
        }

        public Task<bool> TryRemoveAsync(CacheKey key)
        {
            return FinishInTimeOrDie(DoTryRemoveAsync(key));
        }

        private Task<bool> DoTryRemoveAsync(CacheKey key)
        {
            return _database.KeyDeleteAsync(key.HashBase64);
        }

        public Task ClearAsync()
        {
            throw new NotSupportedException("Currently not supported by StackExchange.Redis. Use redis-cli.exe"); 
        }
	}
}
