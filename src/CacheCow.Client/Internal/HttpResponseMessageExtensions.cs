using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Client.Headers;

namespace CacheCow.Client.Internal
{
	internal static class HttpResponseMessageExtensions
	{
		public static HttpResponseMessage AddCacheCowHeader(this HttpResponseMessage response,
			CacheCowHeader header)
		{
			response.Headers.Add(CacheCowHeader.Name, header.ToString());
			return response;
		}


	}
}
