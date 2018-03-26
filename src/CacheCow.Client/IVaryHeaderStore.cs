using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CacheCow.Common;

namespace CacheCow.Client
{
	public interface IVaryHeaderStore : IDisposable
	{
		bool TryGetValue(string uri, out IEnumerable<string> headers);
		void AddOrUpdate(string uri, IEnumerable<string> headers);
		bool TryRemove(string uri);
		void Clear();
	}
}
