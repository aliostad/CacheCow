using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using CacheCow.Common.Helpers;

namespace CacheCow.Client.FileCacheStore.Tests
{
	class RandomResponseBuilder : HttpMessageHandler
	{
		private Random _random = new Random();
		private Dictionary<int, HttpResponseMessage> _responses = new Dictionary<int, HttpResponseMessage>();
		private SHA1CryptoServiceProvider _sha1 = new SHA1CryptoServiceProvider();
		private int _totalResponses;

		public RandomResponseBuilder() : this(100)
		{
			
		}

		public RandomResponseBuilder(int totalResponses)
		{
			_totalResponses = totalResponses;
			for (int i = 0; i < totalResponses; i++)
			{
				_responses.Add(i, BuildRandomResponse());
			}
		}

		private HttpResponseMessage BuildRandomResponse()
		{
			var response = new HttpResponseMessage( HttpStatusCode.OK);
			var bytes = new byte[_random.Next(100000)];
			_random.NextBytes(bytes);
			response.Content = new ByteArrayContent(bytes);
			response.Content.Headers.Add("content-type", "application/octet-stream");
			return response;
		}


		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
			CancellationToken cancellationToken)
		{
			return Send(request).ToTask();
		}

		public HttpResponseMessage Send(HttpRequestMessage request)
		{
			var hash = _sha1.ComputeHash(Encoding.UTF8.GetBytes(request.RequestUri.ToString()));
			var i = Math.Abs( BitConverter.ToInt32(hash, 0));
			var response = _responses[i % _totalResponses];
			response.RequestMessage = request;
			return response;
		}
	}
}
