using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CacheCow.Common;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Results;

namespace CacheCow.Server.EntityTagStore.Memcached
{
    public class MemcachedEntityTagStore : IEntityTagStore, IDisposable
    {

        private readonly MemcachedClient _memcachedClient;

        public MemcachedEntityTagStore()
        {
            _memcachedClient = new MemcachedClient();
        }

         /// <summary>
        /// 
        /// </summary>
        /// <param name="sectionName">Configuration section name</param>
        public MemcachedEntityTagStore(string sectionName)
        {
            _memcachedClient = new MemcachedClient(sectionName);
        }

        public MemcachedEntityTagStore(IMemcachedClientConfiguration configuration)
        {
            _memcachedClient = new MemcachedClient(configuration);
        }

        public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
        {
            eTag = null;
            var operationResult = _memcachedClient.ExecuteGet<string>(key.HashBase64);
            if (!operationResult.Success || !operationResult.HasValue ||
                string.IsNullOrEmpty(operationResult.Value))
            {
                return false;                
            }

            var value = operationResult.Value;
            if (!TimedEntityTagHeaderValue.TryParse(value, out eTag))
                return false;

            return true;

        }

        // TODO: !!! routePattern implementation needs to be changed to Cas

        public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag)
        {
            // add item
            _memcachedClient.ExecuteStore(StoreMode.Set, key.HashBase64, eTag.ToString());

            // add route pattern if not there
            string keyForRoutePattern = GetKeyForRoutePattern(key.RoutePattern);
            string keyForResourceUri = GetKeyForResourceUri(key.ResourceUri);
            var routePatternEntries = GetRoutePatternEntries(key.RoutePattern);
            var resourceUriEntries = GetResourceUriEntries(key.ResourceUri);

           
            if (!routePatternEntries.Contains(key.HashBase64))
            {
                var bytes = new List<byte>();
                foreach (var routePatternEntry in routePatternEntries)
                {
                    bytes.AddRange(new LengthedPrefixedString(routePatternEntry).ToByteArray());
                }
                bytes.AddRange(new LengthedPrefixedString(key.HashBase64).ToByteArray());
                _memcachedClient.ExecuteStore(StoreMode.Set, keyForRoutePattern, bytes.ToArray());

            }

            if (!resourceUriEntries.Contains(key.HashBase64))
            {
                var bytes = new List<byte>();
                foreach (var routePatternEntry in resourceUriEntries)
                {
                    bytes.AddRange(new LengthedPrefixedString(routePatternEntry).ToByteArray());
                }
                bytes.AddRange(new LengthedPrefixedString(key.HashBase64).ToByteArray());
                _memcachedClient.ExecuteStore(StoreMode.Set, keyForResourceUri, bytes.ToArray());

            }


        }

        public int RemoveResource(string resourceUri)
        {
            return Remove(GetResourceUriEntries(resourceUri));
        }
        
        private int Remove(IEnumerable<string> entries)
        {
            int count = 0;
            foreach (var entry in entries)
            {
                var removed = _memcachedClient.Remove(entry);
                if (removed)
                    count++;
            }
            return count;
        }


        internal static string GetKeyForRoutePattern(string routePattern)
        {
            return "___ROUTE_PATTERN___" + routePattern;
        }

        internal static string GetKeyForResourceUri(string resourceUri)
        {
            return "___RESOURCE_URI___" + resourceUri;
        }

        private IEnumerable<string> GetResourceUriEntries(string resourceUri)
        {
            return GetEntries(GetKeyForResourceUri(resourceUri));
        }

        private IEnumerable<string> GetRoutePatternEntries(string routePattern)
        {
            return GetEntries(GetKeyForRoutePattern(routePattern));
        }

        private IEnumerable<string> GetEntries(string key)
        {
            var list = new List<string>();
            var bytes = _memcachedClient.Get<byte[]>(key);
            if (bytes == null)
                return list;
            LengthedPrefixedString prefixedString;
            var memoryStream = new MemoryStream(bytes);
            while (LengthedPrefixedString.TryRead(memoryStream, out prefixedString))
            {
                list.Add(prefixedString.InternalString);
            }

            return list;
        }



        // TODO: !!! routePattern implementation needs to be changed to Cas
        public bool TryRemove(CacheKey key)
        {
            // remove item
            var executeRemove = _memcachedClient.ExecuteRemove(key.HashBase64);

            // remove from routePatterns
            var routePatternEntries = GetRoutePatternEntries(key.RoutePattern);
            var oldCount = routePatternEntries.Count();
            routePatternEntries = routePatternEntries.Where(x => x != key.HashBase64);
            if (routePatternEntries.Count() == oldCount)
                return executeRemove.Success;

            var bytes = new List<byte>();
            foreach (var routePatternEntry in routePatternEntries)
            {
                bytes.AddRange(new LengthedPrefixedString(routePatternEntry).ToByteArray());
            }

            string keyForRoutePattern = GetKeyForRoutePattern(key.RoutePattern);
            _memcachedClient.ExecuteStore(StoreMode.Set, keyForRoutePattern, bytes.ToArray());

            return executeRemove.Success;
        }

        public int RemoveAllByRoutePattern(string routePattern)
        {
            return Remove(GetRoutePatternEntries(routePattern));
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
