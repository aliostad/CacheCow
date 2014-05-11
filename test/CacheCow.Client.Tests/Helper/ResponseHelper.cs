using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using CacheCow.Common.Helpers;

namespace CacheCow.Client.Tests.Helper
{
	internal static class ResponseHelper
	{

        private const string HeadersResourcePath = "CacheCow.Client.Tests.Helper.Headers.txt";
		private static string _sampleHeaders;

		public static HttpResponseMessage GetMessage(HttpContent content)
		{
			return GetMessage(content, GetSampleHeaders());
		}

		public static HttpResponseMessage GetMessage(HttpContent content, string headers)
		{
			var httpResponseMessage = new HttpResponseMessage();
			httpResponseMessage.Headers.Parse(headers);
			httpResponseMessage.Content = content;
			return httpResponseMessage;
		}

		private static string GetSampleHeaders()
		{
			if(_sampleHeaders==null)
			{				
				var manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(HeadersResourcePath);
				byte[] bytes = new byte[manifestResourceStream.Length];
				manifestResourceStream.Read(bytes, 0, bytes.Length);
				_sampleHeaders = Encoding.UTF8.GetString(bytes);
			}
			return _sampleHeaders;
		}

	}
}
