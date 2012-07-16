using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CacheCow.Common;

namespace CacheCow.Client
{
	public class InMemoryVaryHeaderStore : IVaryHeaderStore
	{

		private readonly ConcurrentDictionary<string , string[]> _varyHeaderCache = new ConcurrentDictionary<string, string[]>();


		public bool TryGetValue(string uri, out IEnumerable<string> headers)
		{
			string[] hdrs;
			bool result = _varyHeaderCache.TryGetValue(uri, out hdrs);
			headers = hdrs;
			return result;
		}

		public void AddOrUpdate(string uri, IEnumerable<string> headers)
		{
			_varyHeaderCache.AddOrUpdate(uri, headers.ToArray(),
			                             (key, hdrs) => hdrs);
		}

		public bool TryRemove(string uri)
		{
			string[] hdrs;
			return _varyHeaderCache.TryRemove(uri, out hdrs);
		}

		public void Clear()
		{
			_varyHeaderCache.Clear();
		}
	}
}
