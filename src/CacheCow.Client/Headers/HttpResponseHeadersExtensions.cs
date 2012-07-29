using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Client.Headers
{
	// TODO: change the header to stron
	public static class HttpResponseHeadersExtensions
	{
		public static CacheCowHeader GetCacheCowHeader(this HttpResponseHeaders headers)
		{
			CacheCowHeader header = null;
			var cacheCowHeader = headers.Where(x => x.Key == CacheCowHeader.Name).FirstOrDefault();

			if(cacheCowHeader.Value.Count() > 0)
			{
				var last = cacheCowHeader.Value.Last();
				CacheCowHeader.TryParse(last, out header);
			}

			return header;
		}

	}
}
