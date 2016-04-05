using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Common;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;

namespace CacheCow.Client.MemcachedCacheStore12
{
    public class MemcachedStore : ICacheStore, IDisposable
    {
        private IHttpMessageSerializerAsync _serializer = new MessageContentHttpMessageSerializer();
        private readonly MemcachedClient _memcachedClient;

        public MemcachedStore()
        {
            _memcachedClient = new MemcachedClient();
        }

        public MemcachedStore(IMemcachedClientConfiguration configuration)
        {
            _memcachedClient = new MemcachedClient(configuration);
        }

        public MemcachedStore(string sectionName)
        {
            _memcachedClient = new MemcachedClient(sectionName);
        }

        public bool TryGetValue(CacheKey key, out HttpResponseMessage response)
        {
            response = null;
            var operationResult = _memcachedClient.Get<byte[]>(key.HashBase64);
            if (operationResult != null)
            {
                var ms = new MemoryStream(operationResult);
                response = _serializer.DeserializeToResponseAsync(ms).Result;
            }

            return operationResult != null;
        }

        public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
        {
            var ms = new MemoryStream();

            _serializer.SerializeAsync(TaskHelpers.FromResult(response), ms)
                .Wait();

            _memcachedClient.Store(StoreMode.Set, key.HashBase64, ms.ToArray());
        }

        public bool TryRemove(CacheKey key)
        {
            return _memcachedClient.Remove(key.HashBase64);
        }

        public void Clear()
        {
            _memcachedClient.FlushAll();
        }

        public void Dispose()
        {
            _memcachedClient.Dispose();
        }
    }


}
