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
	    private static TimeSpan DefaultCacheExpiry = TimeSpan.FromHours(6);

#if NET452
        private MemoryCache _responseCache = new MemoryCache(CacheStoreEntryName);  
#else
        private MemoryCache _responseCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));       
#endif

        private MessageContentHttpMessageSerializer _messageSerializer = new MessageContentHttpMessageSerializer(true);
	    private readonly TimeSpan _defaultExpiry;

	    public InMemoryCacheStore()
            : this(DefaultCacheExpiry)
        {
            
        }

        public InMemoryCacheStore(TimeSpan defaultExpiry)
        {
            _defaultExpiry = defaultExpiry;
        }

	    public void Dispose()
	    {
	        _responseCache.Dispose();
	    }

	    public async Task<HttpResponseMessage> GetValueAsync(CacheKey key)
	    {
            var result = _responseCache.Get(key.HashBase64);
	        if (result == null)
	            return null;

	        return (await _messageSerializer.DeserializeToResponseAsync(new MemoryStream((byte[]) result)));
	    }

	    public async Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
	    {
            // removing reference to request so that the request can get GCed
            var req = response.RequestMessage;
            response.RequestMessage = null;
            var memoryStream = new MemoryStream();
	        await _messageSerializer.SerializeAsync(response, memoryStream);
            response.RequestMessage = req;
            _responseCache.Set(key.HashBase64, memoryStream.ToArray(), response.GetExpiry() ?? DateTimeOffset.UtcNow.Add(_defaultExpiry));
	    }

	    public Task<bool> TryRemoveAsync(CacheKey key)
	    {
#if NET452
            return Task.FromResult(_responseCache.Remove(key.HashBase64) != null);
#else
            _responseCache.Remove(key.HashBase64);
            return Task.FromResult(true);
#endif
        }

	    public Task ClearAsync()
	    {
            _responseCache.Dispose();
#if NET452
            _responseCache = new MemoryCache(CacheStoreEntryName);  
#else
            _responseCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));       
#endif
	        return Task.FromResult(0);
	    }
	}
}
