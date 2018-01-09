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
                Init(ConnectionMultiplexer.Connect(connectionString), 
                    databaseId, timeoutMilli, throwExceptions);
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
            int databaseId = 0,
            int timeoutMilli = 10000,
            bool throwExceptions = true)
        {
            Init(connection, databaseId, timeoutMilli, throwExceptions);
        }

        public RedisStore(IDatabase database,
            int timeoutMilli = 10000,
            bool throwExceptions = true)
        {
            _database = database;
            _throwExceptions = throwExceptions;
            _timeoutMilli = timeoutMilli;
        }

        private void Init(ConnectionMultiplexer connection, 
            int databaseId = 0,
            int timeoutMilli = 10000,
            bool throwExceptions = true)
        {
            _connection = connection;
            _database = _connection.GetDatabase(databaseId);
            _throwExceptions = throwExceptions;
            _timeoutMilli = timeoutMilli;
        }

        public void Dispose()
		{
			if (_connection != null && _dispose)
				_connection.Dispose();
		}

        // has to have async for the purpose of exception handling
        public async Task<HttpResponseMessage> GetValueAsync(CacheKey key)
        {
            try
            {
                return await DoGetValueAsync(key);
            }
            catch (Exception e)
            {
                if (_throwExceptions)
                    throw;
                else
                    Trace.WriteLine(e.ToString());
                return null;
            }
        }

	    private async Task<HttpResponseMessage> DoGetValueAsync(CacheKey key)
	    {
            HttpResponseMessage result = null;
            string entryKey = key.HashBase64;

            if (!await _database.KeyExistsAsync(entryKey))
                return null;

            byte[] value = await _database.StringGetAsync(entryKey);
            var memoryStream = new MemoryStream(value);
            var r = await _serializer.DeserializeToResponseAsync(memoryStream);

            return r;
        }

        // has to have async for the purpose of exception handling
        public async Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
        {
            try
            {
                await DoAddOrUpdateAsync(key, response);
            }
            catch(Exception e)
            {
                if (_throwExceptions)
                    throw;
                else
                    Trace.WriteLine(e.ToString());
            }
        }

	    private async Task<bool> DoAddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
	    {
            var memoryStream = new MemoryStream();
            await _serializer.SerializeAsync(response, memoryStream);
            memoryStream.Position = 0;
            var data = memoryStream.ToArray();
            var expiry = response.GetExpiry() ?? DateTimeOffset.UtcNow.AddDays(1);
            var now = DateTimeOffset.UtcNow;
            if (expiry <= now)
            {
                // NOTE: Eventhough the expiry might be now or maxage=0, there is still
                // benefit in storing so you can do conditional get after expiry
                expiry = DateTimeOffset.UtcNow.AddHours(6);
            }

            await _database.StringSetAsync(key.HashBase64, data, expiry.Subtract(now));
            return true;
        }

        // has to have async for the purpose of exception handling
        public async Task<bool> TryRemoveAsync(CacheKey key)
        {
            try
            {
                return await DoTryRemoveAsync(key);
            }
            catch (Exception e)
            {
                if (_throwExceptions)
                    throw;
                else
                    Trace.WriteLine(e.ToString());
                return false;
            }
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
