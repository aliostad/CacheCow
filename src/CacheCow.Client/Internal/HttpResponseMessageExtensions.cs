using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var previousCacheCowHeader = response.Headers.GetCacheCowHeader();
            if (previousCacheCowHeader != null)
            {
                TraceWriter.WriteLine("WARNING: Already had this header: {0} NOw setting this: {1}", TraceLevel.Warning, previousCacheCowHeader, header);
                response.Headers.Remove(CacheCowHeader.Name);
            }

            response.Headers.Add(CacheCowHeader.Name, header.ToString());
            return response;
        }

        public static HttpResponseMessage CopyOtherCacheCowHeaders(this HttpResponseMessage response, HttpResponseMessage other)
        {
            foreach (var h in other.Headers)
            {
                if(h.Key.StartsWith("x-cachecow"))
                {
                    if(response.Headers.Contains(h.Key))
                        response.Headers.Remove(h.Key);

                    response.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
            }

            return response;
        }

    }
}
