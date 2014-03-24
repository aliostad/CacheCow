namespace CacheCow.Client.AzureCachingCacheStore
{
	using System.IO;
	using System.Net.Http;
	using System.Threading.Tasks;

	using CacheCow.Common;

	using Microsoft.ApplicationServer.Caching;

	public class AzureCacheStore : ICacheStore
	{
		private readonly DataCache cache;
		private const string CacheRegion = "CacheCowClient";
		private IHttpMessageSerializerAsync serializer = new MessageContentHttpMessageSerializer();

		public AzureCacheStore()
		{
			cache = new DataCache("CacheCow");
			cache.CreateRegion(CacheRegion);
		}

		public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
			var ms = new MemoryStream();

            this.serializer.SerializeAsync(TaskHelpers.FromResult(response), ms)
                .Wait();

			this.cache.Add(key.HashBase64, ms.ToArray(), CacheRegion);
		}

		public void Clear()
		{
			this.cache.ClearRegion(CacheRegion);
		}

		public void Dispose()
		{
			// nothing
		}

		public bool TryGetValue(CacheKey key, out HttpResponseMessage response)
		{
			var cacheObject = cache.Get(key.HashBase64, CacheRegion) as byte[];

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
			return cache.Remove(key.HashBase64);
		}
	}
}