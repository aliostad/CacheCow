using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CacheCow
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
