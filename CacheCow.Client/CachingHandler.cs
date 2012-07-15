using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CacheCow.Common;

namespace CacheCow.Client
{
	public class CachingHandler : DelegatingHandler
	{

		private readonly ICacheStore _cacheStore;

		public CachingHandler():this(new InMemoryCacheStore())
		{
		}

		public CachingHandler(ICacheStore cacheStore)
		{
			_cacheStore = cacheStore;
			VaryHeaderStore = new InMemoryVaryHeaderStore();
			DefaultVaryHeaders = new string[]{"Accept"};
		}

		public IVaryHeaderStore VaryHeaderStore { get; set; }

		public string[] DefaultVaryHeaders { get; set; }

		public string[] StarVaryHeaders { get; set; } // TODO: populate and use

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			string uri = request.RequestUri.ToString();
			IEnumerable<string> varyHeaders;
			if(!VaryHeaderStore.TryGetValue(uri, out varyHeaders))
			{
				varyHeaders = DefaultVaryHeaders;
			}
			var cacheKey = new CacheKey(uri, 
				request.Headers.Where(x=> varyHeaders.Any(y=> y.Equals(x.Key, 
					StringComparison.CurrentCultureIgnoreCase)))
					.SelectMany(z=>z.Value)
				);

			HttpResponseMessage response;
			if(_cacheStore.TryGetValue(cacheKey, out response))
			{
				response.RequestMessage = request;
				var taskCompletionSource = new TaskCompletionSource<HttpResponseMessage>();
				taskCompletionSource.SetResult(response);
				return taskCompletionSource.Task;
			}
			// TODO: ..... REST)


			return base.SendAsync(request, cancellationToken);
		}
	}
}
