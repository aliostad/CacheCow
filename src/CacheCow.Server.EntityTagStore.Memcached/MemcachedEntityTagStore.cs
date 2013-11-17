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
            var routePatternEntries = GetRoutePatternEntries(key.RoutePattern);

           
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


        }

        public int RemoveResource(string url)
        {
            throw new NotImplementedException();
        }

        internal static string GetKeyForRoutePattern(string routePattern)
        {
            return "___ROUTE_PATTERN___" + routePattern;
        }

        private IEnumerable<string> GetRoutePatternEntries(string routePattern)
        {
            var list = new List<string>();
            string keyForRoutePattern = GetKeyForRoutePattern(routePattern);
            var bytes = _memcachedClient.Get<byte[]>(keyForRoutePattern);
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
            int count = 0;
            var routePatternEntries = GetRoutePatternEntries(routePattern);
            foreach (var routePatternEntry in routePatternEntries)
            {
                var removed = _memcachedClient.Remove(routePatternEntry);
                if (removed)
                    count++;
            }
            return count;
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
