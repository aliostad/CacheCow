using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Common.Helpers
{
	public static class HttpHeadersExtensions
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="httpHeaders">Headers</param>
		/// <param name="headers">CR-LF separated headers as specified in HTTP spec</param>
		public static void Parse(this HttpHeaders httpHeaders, string headers)
		{
			if (httpHeaders == null)
				throw new ArgumentNullException("httpHeaders");

			if (headers == null)
				throw new ArgumentNullException("headers");

			string name = null, value = null;
			foreach (var header in headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
			{
				var indexOfColon = header.IndexOf(":");
				name = header.Substring(0, indexOfColon);
				value = header.Substring(indexOfColon + 1).Trim();
				if(!httpHeaders.TryAddWithoutValidation(name, value))
					throw new InvalidOperationException(string.Format("Value {0} for header {1} not acceptable.", value, name));
			}

		}


        /// <summary>
        /// Extracts all headers provided. Does a case-insensitive selection.
        /// </summary>
        /// <param name="headers">this parameter</param>
        /// <param name="headerNames">Name of the headers</param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<string, IEnumerable<string>>> ExtractHeadersValues(
            this HttpRequestHeaders headers, 
            params string[] headerNames)
        {
            return headers.Where(x =>
                headerNames.Any(h => h.Equals(x.Key, StringComparison.CurrentCultureIgnoreCase)));
        }
	}
}
