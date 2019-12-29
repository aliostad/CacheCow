using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Common;
using System.Threading.Tasks;
using CacheCow.Common.Helpers;

#if NET452
using System.Runtime.Caching;
#else
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
#endif

namespace CacheCow.Client
{
	public class InMemoryCacheStore : ICacheStore
	{
        private const string CacheStoreEntryName = "###InMemoryCacheStore_###";
	    private static TimeSpan MinCacheExpiry = TimeSpan.FromHours(6);
        private MemoryCache _responseCache;

        private MessageContentHttpMessageSerializer _messageSerializer = new MessageContentHttpMessageSerializer(true);
	    private readonly TimeSpan _minExpiry;

	    public InMemoryCacheStore()
            : this(MinCacheExpiry)
        {

        }

        public InMemoryCacheStore(TimeSpan minExpiry) :
#if NET452
            this(minExpiry, new MemoryCache(CacheStoreEntryName))
#else
            this(minExpiry, new MemoryCache(Options.Create(new MemoryCacheOptions())))
#endif
        {
        }

        public InMemoryCacheStore(TimeSpan minExpiry, MemoryCache cache)
        {
            _minExpiry = minExpiry;
            _responseCache = cache;
        }

        /// <inheritdoc />
        public void Dispose()
	    {
	        _responseCache.Dispose();
	    }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetValueAsync(CacheKey key)
	    {
            var result = _responseCache.Get(key.HashBase64);
	        if (result == null)
	            return null;

	        return await _messageSerializer.DeserializeToResponseAsync(new MemoryStream((byte[]) result)).ConfigureAwait(false);
	    }

        /// <inheritdoc />
        public async Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
	    {
            // removing reference to request so that the request can get GCed
            var req = response.RequestMessage;
            response.RequestMessage = null;
            var memoryStream = new MemoryStream();
	        await _messageSerializer.SerializeAsync(response, memoryStream).ConfigureAwait(false);
            response.RequestMessage = req;
            var suggestedExpiry = response.GetExpiry() ?? DateTimeOffset.UtcNow.Add(_minExpiry);
            var minExpiry = DateTimeOffset.UtcNow.Add(_minExpiry);
            var optimalExpiry = (suggestedExpiry > minExpiry) ? suggestedExpiry : minExpiry;
            _responseCache.Set(key.HashBase64, memoryStream.ToArray(), optimalExpiry);
	    }

        /// <inheritdoc />
        public Task<bool> TryRemoveAsync(CacheKey key)
	    {
#if NET452
            return Task.FromResult(_responseCache.Remove(key.HashBase64) != null);
#else
            _responseCache.Remove(key.HashBase64);
            return Task.FromResult(true);
#endif
        }

        /// <inheritdoc />
        public Task ClearAsync()
	    {
            _responseCache.Dispose();
#if NET452
            _responseCache = new MemoryCache(CacheStoreEntryName);
#else
            // WARNING: !! If you have passed one, then this one will not have the options
            _responseCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
#endif
	        return Task.FromResult(0);
	    }
	}
}
