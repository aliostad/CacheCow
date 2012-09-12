using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Common.Http
{
	public class NotModifiedResponse : HttpResponseMessage
	{
		public NotModifiedResponse(HttpRequestMessage request)
			: this(request, null)
		{
		}


		public NotModifiedResponse(HttpRequestMessage request, EntityTagHeaderValue etag)
			: base(HttpStatusCode.NotModified)
		{
			if(etag!=null)
				this.Headers.ETag = etag;

			this.RequestMessage = request;
		}

	}

	
}
