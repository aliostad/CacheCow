using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
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
        public const string ContentString =
    "When you are done with your work, I recommend you stop the session instead of saving it for later.  To stop screen you can usually just type exit from your shell. This will close that screen window.  You have to close all screen windows to terminate the session.";

        public static HttpResponseMessage GetMessage(HttpContent content)
		{
			return GetMessage(content, GetSampleHeaders());
		}

		private static HttpResponseMessage GetMessage(HttpContent content, string headers)
		{
			var httpResponseMessage = new HttpResponseMessage();
			httpResponseMessage.Headers.Parse(headers);
			httpResponseMessage.Content = content;
			return httpResponseMessage;
		}

		public static string GetSampleHeaders()
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

        public static HttpResponseMessage GetOkMessage(int expirySeconds = 200, bool mustRevalidate = false)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(expirySeconds),
                MustRevalidate = mustRevalidate
            };
            response.Headers.Date = DateTimeOffset.UtcNow;
            response.Content = new StringContent(ContentString);
            return response;
        }

        public static HttpResponseMessage GetNotModifiedMessage(int expirySeconds = 200)
        {
            var response = new HttpResponseMessage(HttpStatusCode.NotModified);
            response.Headers.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(expirySeconds),
                MustRevalidate = true
            };
            response.Headers.Date = DateTimeOffset.UtcNow;
            return response;
        }

    }
}
