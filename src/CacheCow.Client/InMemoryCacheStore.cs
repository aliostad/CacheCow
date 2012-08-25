using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Common;

namespace CacheCow.Client
{
	public class InMemoryCacheStore : ICacheStore
	{
		private readonly ConcurrentDictionary<CacheKey, byte[]> _responseCache = new ConcurrentDictionary<CacheKey, byte[]>();
		private MessageContentHttpMessageSerializer _messageSerializer = new MessageContentHttpMessageSerializer(true);

		public bool TryGetValue(CacheKey key, out HttpResponseMessage response)
		{
			byte[] buffer;
			response = null;
			var result = _responseCache.TryGetValue(key, out buffer);
			if (result)
			{
				response = _messageSerializer.DeserializeToResponseAsync(new MemoryStream(buffer)).Result;
			}
			return result;
		}

		public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
			// removing reference to request so that the request can get GCed
			response.RequestMessage = null;
			var memoryStream = new MemoryStream();
			_messageSerializer.SerializeAsync(TaskHelpers.FromResult(response), memoryStream).Wait();

			_responseCache.AddOrUpdate(key, memoryStream.ToArray(), (ky, old) => memoryStream.ToArray());
		}

		public bool TryRemove(CacheKey key)
		{
			byte[] response;
			return _responseCache.TryRemove(key, out response);
		}

		public void Clear()
		{
			_responseCache.Clear();
		}
	}
}
