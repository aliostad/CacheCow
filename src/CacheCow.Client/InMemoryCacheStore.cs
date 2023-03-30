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

#if NET462
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

#if NET462
#else
        private readonly IOptions<MemoryCacheOptions> _options;
#endif
        public InMemoryCacheStore()
            : this(MinCacheExpiry)
        {

        }

        public InMemoryCacheStore(TimeSpan minExpiry) :
#if NET462
            this(minExpiry, new MemoryCache(CacheStoreEntryName))
#else
            this(minExpiry, Options.Create(new MemoryCacheOptions()))
#endif
        {
        }

        private InMemoryCacheStore(TimeSpan minExpiry, MemoryCache cache)
        {
            _minExpiry = minExpiry;
            _responseCache = cache;
        }

#if NET462
#else
        /// <summary>
        /// To control cache options
        /// </summary>
        /// <param name="minExpiry">expiry</param>
        /// <param name="options">options</param>
        public InMemoryCacheStore(TimeSpan minExpiry, IOptions<MemoryCacheOptions> options) :
            this(minExpiry, new MemoryCache(options))
        {
            _options = options;
        }
#endif


        /// <inheritdoc />
        public void Dispose()
        {
            _responseCache.Dispose();
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetValueAsync(CacheKey key)
        {
            var result = (byte[])_responseCache.Get(key.HashBase64);
            if (result == null)
                return null;

            return await _messageSerializer.DeserializeToResponseAsync(new MemoryStream(result)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
        {
            // removing reference to request so that the request can get GCed
            // UPDATE 2022 - What on earth am I doing it for? I cannot remember.
            var req = response.RequestMessage;
            response.RequestMessage = null;
            var memoryStream = new MemoryStream();
            await _messageSerializer.SerializeAsync(response, memoryStream).ConfigureAwait(false);
            var buffer = memoryStream.ToArray();

            response.RequestMessage = req;
            var suggestedExpiry = response.GetExpiry() ?? DateTimeOffset.UtcNow.Add(_minExpiry);
            var minExpiry = DateTimeOffset.UtcNow.Add(_minExpiry);
            var optimalExpiry = (suggestedExpiry > minExpiry) ? suggestedExpiry : minExpiry;
            _responseCache.Set(key.HashBase64, buffer, optimalExpiry);
        }

        /// <inheritdoc />
        public Task<bool> TryRemoveAsync(CacheKey key)
        {
#if NET462
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
#if NET462
            _responseCache = new MemoryCache(CacheStoreEntryName);
#else
            _responseCache = new MemoryCache(_options);
#endif
            return Task.FromResult(0);
        }
    }
}
