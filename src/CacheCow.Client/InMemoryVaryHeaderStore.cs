using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CacheCow.Common;
#if NET462
using System.Runtime.Caching;
#else
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
#endif

namespace CacheCow.Client
{
    public class InMemoryVaryHeaderStore : IVaryHeaderStore
    {
        private const string CacheName = "###_IVaryHeaderStore_###";
        private readonly ConcurrentDictionary<string, string[]> _varyHeaderCache = new ConcurrentDictionary<string, string[]>();
#if NET462
        private MemoryCache _cache = new MemoryCache(CacheName);  
#else
        private MemoryCache _cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
#endif

        public bool TryGetValue(string uri, out IEnumerable<string> headers)
        {
            headers = (string[])_cache.Get(uri);
            return headers != null;
        }

        public void AddOrUpdate(string uri, IEnumerable<string> headers)
        {
            _cache.Set(uri, headers, DateTimeOffset.MaxValue);
        }

        public bool TryRemove(string uri)
        {
#if NET462
            return _cache.Remove(uri) != null;
#endif
            _cache.Remove(uri);
            return true;
        }

        public void Clear()
        {
            ((IDisposable)_cache).Dispose();
#if NET462
            _cache = new MemoryCache(CacheName);  
#else
            _cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
#endif
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
