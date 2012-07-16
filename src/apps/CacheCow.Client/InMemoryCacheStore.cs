using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Common;

namespace CacheCow.Client
{
	public class InMemoryCacheStore : ICacheStore
	{
		private readonly ConcurrentDictionary<CacheKey, HttpResponseMessage> _responseCache = new ConcurrentDictionary<CacheKey, HttpResponseMessage>();

		public bool TryGetValue(CacheKey key, out HttpResponseMessage response)
		{
			return _responseCache.TryGetValue(key, out response);
		}

		public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
			// removing reference to request so that the request can get GCed
			response.RequestMessage = null;

			_responseCache.AddOrUpdate(key, response, (ky, resp) => resp);
		}

		public bool TryRemove(CacheKey key)
		{
			HttpResponseMessage response;
			return _responseCache.TryRemove(key, out response);
		}

		public void Clear()
		{
			_responseCache.Clear();
		}
	}
}
