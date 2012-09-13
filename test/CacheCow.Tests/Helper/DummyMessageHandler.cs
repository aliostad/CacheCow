using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CacheCow.Common;

namespace CacheCow.Tests.Helper
{
	class DummyMessageHandler : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
			CancellationToken cancellationToken)
		{
			Request = request;
			return TaskHelpers.FromResult(Response);
		}

		public HttpRequestMessage Request { get; set; }

		public HttpResponseMessage Response { get; set; }

	}
}
