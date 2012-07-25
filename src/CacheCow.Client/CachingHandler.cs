using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
			CachedMessageValidator = (response) =>
			    {
					if (!response.IsSuccessStatusCode || response.Headers.CacheControl == null ||
						response.Headers.CacheControl.NoStore || response.Headers.CacheControl.NoCache)
						return CacheValidationResult.Invalid;

					response.Headers.Date = response.Headers.Date ?? DateTimeOffset.UtcNow; // this also helps in cache creation
			        var dateTimeOffset = response.Headers.Date;

					if (response.Headers.CacheControl.MustRevalidate)
						return CacheValidationResult.MustRevalidate;

					if (response.Content.Headers.Expires != null &&
						response.Content.Headers.Expires < DateTimeOffset.UtcNow)
						return CacheValidationResult.Stale;

					if (response.Headers.CacheControl.MaxAge == null &&
						response.Headers.CacheControl.SharedMaxAge == null)
						return CacheValidationResult.Invalid;

					if (response.Headers.CacheControl.MaxAge != null &&
						DateTimeOffset.UtcNow > response.Headers.Date.Value.Add(response.Headers.CacheControl.MaxAge.Value))
						return CacheValidationResult.Stale;

					if (response.Headers.CacheControl.SharedMaxAge != null &&
						DateTimeOffset.UtcNow > response.Headers.Date.Value.Add(response.Headers.CacheControl.SharedMaxAge.Value))
						return CacheValidationResult.Stale;


			        return CacheValidationResult.OK;
			    };
		}

		public IVaryHeaderStore VaryHeaderStore { get; set; }

		public string[] DefaultVaryHeaders { get; set; }

		public string[] StarVaryHeaders { get; set; } // TODO: populate and use

		public Func<HttpResponseMessage, CacheValidationResult> CachedMessageValidator { get; set; }

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
