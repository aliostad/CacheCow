using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CacheCow.Common;
using CacheCow.Common.Helpers;

namespace CacheCow.Client
{
	public class CachingHandler : DelegatingHandler
	{

		private readonly ICacheStore _cacheStore;
		private Func<HttpRequestMessage, bool> _ignoreRequestRules;
		
		// 13.4: A response received with a status code of 200, 203, 206, 300, 301 or 410 MAY be stored 
		private static HttpStatusCode[] _cacheableStatuses = new HttpStatusCode[]
		    {
				HttpStatusCode.OK, HttpStatusCode.NonAuthoritativeInformation,
       			HttpStatusCode.PartialContent, HttpStatusCode.MultipleChoices,
				HttpStatusCode.MovedPermanently, HttpStatusCode.Gone
			};

		public CachingHandler():this(new InMemoryCacheStore())
		{
		}

		public CachingHandler(ICacheStore cacheStore)
		{
			_cacheStore = cacheStore;
			UseConditionalPut = true;
			VaryHeaderStore = new InMemoryVaryHeaderStore();
			DefaultVaryHeaders = new string[]{"Accept"};
			ResponseValidator = (response) =>
			    {
					if (!response.IsSuccessStatusCode || response.Headers.CacheControl == null ||
						response.Headers.CacheControl.NoStore || response.Headers.CacheControl.NoCache)
						return ResponseValidationResult.Invalid;

					response.Headers.Date = response.Headers.Date ?? DateTimeOffset.UtcNow; // this also helps in cache creation
			        var dateTimeOffset = response.Headers.Date;

					if(response.Content == null)
						return ResponseValidationResult.Invalid;

					if (response.Headers.CacheControl.MaxAge == null &&
						response.Headers.CacheControl.SharedMaxAge == null &&
						response.Content.Headers.Expires == null)
						return ResponseValidationResult.Invalid;

					if (response.Content.Headers.Expires != null &&
						response.Content.Headers.Expires < DateTimeOffset.UtcNow)
						return ResponseValidationResult.Stale;

					if (response.Headers.CacheControl.MaxAge != null &&
						DateTimeOffset.UtcNow > response.Headers.Date.Value.Add(response.Headers.CacheControl.MaxAge.Value))
						return ResponseValidationResult.Stale;

					if (response.Headers.CacheControl.SharedMaxAge != null &&
						DateTimeOffset.UtcNow > response.Headers.Date.Value.Add(response.Headers.CacheControl.SharedMaxAge.Value))
						return ResponseValidationResult.Stale;

					if (response.Headers.CacheControl.MustRevalidate)
						return ResponseValidationResult.MustRevalidate;


			        return ResponseValidationResult.OK;
			    };

			_ignoreRequestRules = (request) =>
			    {

					if (!request.Method.IsIn(HttpMethod.Get, HttpMethod.Post))
						return true;

					// client can tell CachingHandler not to do caching for a particular request
					if(request.Headers.CacheControl!=null)
					{
						if (request.Headers.CacheControl.NoCache || request.Headers.CacheControl.NoStore)
							return true;
					}

			        return false;
			    };
		}

		public IVaryHeaderStore VaryHeaderStore { get; set; }

		public string[] DefaultVaryHeaders { get; set; }

		public string[] StarVaryHeaders { get; set; } // TODO: populate and use

		/// <summary>
		/// Whether to use cache's ETag or Last-Modified
		/// to make conditional PUT according to RFC2616 13.3
		/// If no cache available on the resource, no conditional is used
		/// </summary>
		public bool UseConditionalPut { get; set; }

		public Func<HttpResponseMessage, ResponseValidationResult> ResponseValidator { get; set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			string uri = request.RequestUri.ToString();

			// check if needs to be ignored
			if (_ignoreRequestRules(request))
				return base.SendAsync(request, cancellationToken); // EXIT !! _________________


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

			// _____________________________
			// TODO: Do PUT stuff here
			// check if UseValidation and add headers and let things run normally, no continuation
			// _________________


			// here onward is only GET
			HttpResponseMessage cachedResponse;
			ResponseValidationResult validationResultForCachedResponse = ResponseValidationResult.NotExist;
			if(_cacheStore.TryGetValue(cacheKey, out cachedResponse))
			{
				cachedResponse.RequestMessage = request;

				validationResultForCachedResponse = ResponseValidator(cachedResponse);
				if(validationResultForCachedResponse ==ResponseValidationResult.OK)
					return TaskHelpers.FromResult(cachedResponse); // EXIT !! ____________________________

			}

			if(validationResultForCachedResponse == ResponseValidationResult.MustRevalidate)
			{
				// TODO: 
				// add headers for a cache validation 
			}

			

			return base.SendAsync(request, cancellationToken)
				.ContinueWith(
				task =>			
					{
						var serverResponse = task.Result;
						if (request.Method != HttpMethod.Get) // only interested here if it is a GET
							return serverResponse;

						// in case of MustRevalidate with result 304
						if(validationResultForCachedResponse == ResponseValidationResult.MustRevalidate && 
							serverResponse.StatusCode == HttpStatusCode.NotModified)
						{
							cachedResponse.RequestMessage = request;
							return cachedResponse; // EXIT !! _______________
						}

						var validationResult = ResponseValidator(serverResponse);
						switch (validationResult)
						{
							// TODO: Here just store. 
							// If invalid and exists in store then clear it!
							//case ResponseValidationResult.
						}
						return serverResponse;
					}
				);
		}


	}
}
