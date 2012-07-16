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
		public NotModifiedResponse()
			: base(HttpStatusCode.NotModified)
		{
		}


		public NotModifiedResponse(EntityTagHeaderValue etag)
			: base(HttpStatusCode.NotModified)
		{
			this.Headers.ETag = etag;
		}

	}

	
}
