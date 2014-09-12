namespace CacheCow.Client.AzureCachingCacheStore
{
	using System.IO;
	using System.Net.Http;
	using System.Threading.Tasks;

	using CacheCow.Common;

	using Microsoft.ApplicationServer.Caching;

	public class AzureCacheStore : ICacheStore
	{
		private readonly DataCache _cache;
		private const string DefaultCacheRegion = "CacheCowClient";
		private IHttpMessageSerializerAsync serializer = new MessageContentHttpMessageSerializer();
	    private string _cacheRegion;


	    /// <summary>
        /// Default cacheName is "default"
        /// </summary>
		public AzureCacheStore()
            : this("default")
		{
			
		}

        public AzureCacheStore(string cacheName)
            : this(cacheName, DefaultCacheRegion)

        {
            
        }

        public AzureCacheStore(string cacheName, string cacheRegion)
        {
            _cacheRegion = cacheRegion;
            _cache = new DataCache(cacheName);
            _cache.CreateRegion(_cacheRegion);
        }

	    public AzureCacheStore(DataCache cache)
            : this(cache, DefaultCacheRegion)
	    {
	    }

	    public AzureCacheStore(DataCache cache, string cacheRegion)
        {
            _cacheRegion = cacheRegion;
	        _cache = cache;
            _cache.CreateRegion(_cacheRegion);
        }



		public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
			var ms = new MemoryStream();

            this.serializer.SerializeAsync(TaskHelpers.FromResult(response), ms)
                .Wait();

            this._cache.Put(key.HashBase64, ms.ToArray(), _cacheRegion);
		}

		public void Clear()
		{
            this._cache.ClearRegion(_cacheRegion);
		}

		public void Dispose()
		{
			// nothing
		}

		public bool TryGetValue(CacheKey key, out HttpResponseMessage response)
		{
            var cacheObject = _cache.Get(key.HashBase64, _cacheRegion) as byte[];

			if (cacheObject != null)
			{
				var ms = new MemoryStream(cacheObject);
				response = serializer.DeserializeToResponseAsync(ms).Result;

				return true;
			}

			response = null;

			return false;
		}

		public bool TryRemove(CacheKey key)
		{
			return _cache.Remove(key.HashBase64);
		}
	}
}