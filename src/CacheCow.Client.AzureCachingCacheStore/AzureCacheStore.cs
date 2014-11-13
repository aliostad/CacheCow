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
		private const string DefaultCacheRegion = "";
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

            if (!string.IsNullOrEmpty(_cacheRegion))
                _cache.CreateRegion(_cacheRegion);
        }



		public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
			var ms = new MemoryStream();

            this.serializer.SerializeAsync(TaskHelpers.FromResult(response), ms)
                .Wait();

		    if (string.IsNullOrEmpty(_cacheRegion))
                this._cache.Put(key.HashBase64, ms.ToArray());
		    else
                this._cache.Put(key.HashBase64, ms.ToArray(), _cacheRegion);
		}

		public void Clear()
		{
            if (string.IsNullOrEmpty(_cacheRegion))
                this._cache.Clear();
            else
               this._cache.ClearRegion(_cacheRegion);
		}

		public void Dispose()
		{
			// nothing
		}

		public bool TryGetValue(CacheKey key, out HttpResponseMessage response)
		{
		    byte[] cacheObject = null;

            if (string.IsNullOrEmpty(_cacheRegion))
                cacheObject = _cache.Get(key.HashBase64) as byte[];
            else
                cacheObject = _cache.Get(key.HashBase64, _cacheRegion) as byte[];

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
            if (string.IsNullOrEmpty(_cacheRegion))
                return _cache.Remove(key.HashBase64);
            else
			    return _cache.Remove(key.HashBase64, _cacheRegion);
		}
	}
}