using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using CacheCow.Common;
using CacheCow.Common.Helpers;
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
	    private bool _throwExceptions;
        private static TimeSpan DefaultMinLifeTime = TimeSpan.FromHours(6);

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisStore"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="databaseId">The database identifier.</param>
        /// <param name="throwExceptions">if set to <c>true</c> the store will throw exceptions.</param>
        public RedisStore(string connectionString,
            int databaseId = 0,
            bool throwExceptions = true)
	    {
	        _throwExceptions = throwExceptions;
	        try
	        {
                Init(ConnectionMultiplexer.Connect(connectionString),
                    databaseId, throwExceptions);
            }
	        catch (Exception e)
	        {
                if(_throwExceptions)
	                throw;
                else
                    Trace.WriteLine(e.ToString());
	        }
	    }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisStore"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="databaseId">The database identifier.</param>
        /// <param name="throwExceptions">if set to <c>true</c> the store will throw exceptions.</param>
        public RedisStore(ConnectionMultiplexer connection,
            int databaseId = 0,
            bool throwExceptions = true)
        {
            Init(connection, databaseId, throwExceptions);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisStore"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="throwExceptions">if set to <c>true</c> the store will throw exceptions.</param>
        public RedisStore(IDatabase database,
            bool throwExceptions = true)
        {
            _database = database;
            _throwExceptions = throwExceptions;
        }

        private void Init(ConnectionMultiplexer connection,
            int databaseId = 0,
            bool throwExceptions = true)
        {
            _connection = connection;
            _dispose = false;
            _database = _connection.GetDatabase(databaseId);
            _throwExceptions = throwExceptions;
        }

        /// <summary>
        /// Minimum expiry of items. Default is 6 hours.
        /// Bear in mind, even expired items can be used if we do a cache validation request and get back 304
        /// </summary>
        public TimeSpan MinExpiry
        {
            get; set;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
		{
			if (_connection != null && _dispose)
				_connection.Dispose();
		}

        /// <summary>
        /// Gets the cached HTTP-Response from this storage
        /// </summary>
        /// <param name="key"></param>
        /// <returns>
        /// The ResponseMessage if the key was found; null otherwise
        /// </returns>
        /// <remarks>has to have async for the purpose of exception handling</remarks>
        public async Task<HttpResponseMessage> GetValueAsync(CacheKey key)
        {
            try
            {
                return await DoGetValueAsync(key).ConfigureAwait(false);
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
            string entryKey = key.HashBase64;

            if (!await _database.KeyExistsAsync(entryKey).ConfigureAwait(false))
                return null;

            byte[] value = await _database.StringGetAsync(entryKey).ConfigureAwait(false);

            // possibility of a race condition, where the object expires between checking if it exists and retrieving it
            // issue #264 - hard to write a test for it
            if (value == null)
                return null;

            var memoryStream = new MemoryStream(value);
            var r = await _serializer.DeserializeToResponseAsync(memoryStream).ConfigureAwait(false);

            return r;
        }

        /// <summary>
        /// Adds the given response to the cachestore. If the key is already present, the old value is overwritten
        /// </summary>
        /// <param name="key"></param>
        /// <param name="response"></param>
        /// <remarks>has to have async for the purpose of exception handling</remarks>
        public async Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
        {
            try
            {
                await DoAddOrUpdateAsync(key, response).ConfigureAwait(false);
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
            await _serializer.SerializeAsync(response, memoryStream).ConfigureAwait(false);
            memoryStream.Position = 0;
            var data = memoryStream.ToArray();
            var expiry = response.GetExpiry() ?? DateTimeOffset.UtcNow.AddDays(1);
            var minExpiry = DateTimeOffset.UtcNow.Add(DefaultMinLifeTime);
            if (expiry <= minExpiry)
            {
                // NOTE: Eventhough the expiry might be now or maxage=0, there is still
                // benefit in storing so you can do conditional get after expiry
                expiry = minExpiry;
            }

            await _database.StringSetAsync(key.HashBase64, data, expiry.Subtract(DateTimeOffset.UtcNow)).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// (Tries to) remove the cached response corresponding with this key from the cache.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>
        /// True if deletion was successfull, False if not (e.g. the key was not in the store to begin with)
        /// </returns>
        /// <remarks>has to have async for the purpose of exception handling</remarks>
        public async Task<bool> TryRemoveAsync(CacheKey key)
        {
            try
            {
                return await DoTryRemoveAsync(key).ConfigureAwait(false);
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

        /// <summary>
        /// Nuke the cache
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Currently not supported by StackExchange.Redis. Use redis-cli.exe</exception>
        public Task ClearAsync()
        {
            throw new NotSupportedException("Currently not supported by StackExchange.Redis. Use redis-cli.exe");
        }
	}
}
