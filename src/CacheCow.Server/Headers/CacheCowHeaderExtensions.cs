#if NET462
#else
using Microsoft.AspNetCore.Http;
#endif
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CacheCow.Server.Headers
{
    public static class CacheCowHeaderExtensions
    {
        /// <summary>
        /// Extracts CacheCowHeader (server) if one exists
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static CacheCowHeader GetCacheCowHeader(
            this HttpResponseMessage response)
        {
            CacheCowHeader header = null;
            if (response.Headers.Contains(CacheCowHeader.Name))
            {
                CacheCowHeader.TryParse(response.Headers.GetValues(CacheCowHeader.Name).FirstOrDefault(), out header);
            }

            return header;
        }

#if NET462
#else
        /// <summary>
        /// Extracts CacheCowHeader (server) if one exists
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static CacheCowHeader GetCacheCowHeader(
            this HttpResponse response)
        {
            CacheCowHeader header = null;
            if (response.Headers.ContainsKey(CacheCowHeader.Name))
            {
                CacheCowHeader.TryParse(response.Headers[CacheCowHeader.Name], out header);
            }

            return header;
        }
#endif
    }
}
