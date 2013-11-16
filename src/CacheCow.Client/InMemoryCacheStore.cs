using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;
using CacheCow.Common;
using System.Threading.Tasks;

namespace CacheCow.Client
{
	public class InMemoryCacheStore : ICacheStore
	{
        private const string CacheStoreEntryName = "###InMemoryCacheStore_###";


        private MemoryCache _responseCache = new MemoryCache(CacheStoreEntryName);
		private MessageContentHttpMessageSerializer _messageSerializer = new MessageContentHttpMessageSerializer(true);

		public bool TryGetValue(CacheKey key, out HttpResponseMessage response)
		{
			response = null;
			var result = _responseCache.Get(key.HashBase64);
			if (result!=null)
			{
                response = _messageSerializer.DeserializeToResponseAsync(new MemoryStream((byte[])result)).Result;
			}
			return result!=null;
		}

		public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
			// removing reference to request so that the request can get GCed
			var req = response.RequestMessage;
			response.RequestMessage = null;
			var memoryStream = new MemoryStream();
			_messageSerializer.SerializeAsync(TaskHelpers.FromResult(response), memoryStream).Wait();
			response.RequestMessage = req;
			_responseCache.Set(key.HashBase64, memoryStream.ToArray(), DateTimeOffset.Now);
		}

		public bool TryRemove(CacheKey key)
		{
			byte[] response;
			return _responseCache.Remove(key.HashBase64) != null;
		}

		public void Clear()
		{
			_responseCache.Dispose();
            _responseCache = new MemoryCache(CacheStoreEntryName);
		}
	}
}
