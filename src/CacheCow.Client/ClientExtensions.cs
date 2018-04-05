using CacheCow.Common;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace CacheCow.Client
{
    public static class ClientExtensions
    {
        /// <summary>
        /// Creates HttpClient with InMemoryCacheStore and HttpClientHandler
        /// </summary>
        /// <returns></returns>
        public static HttpClient CreateClient(HttpMessageHandler handler = null)
        {
            return new HttpClient(new CachingHandler()
            {
                InnerHandler = handler ?? new HttpClientHandler()
            });
        }

        /// <summary>
        /// Creates HttpClient with the store and HttpClientHandler
        /// </summary>
        /// <param name="store"></param>
        /// <param name="handler"></param>
        /// <returns></returns>

        public static HttpClient CreateClient(this ICacheStore store, HttpMessageHandler handler = null)
        {
            return new HttpClient(new CachingHandler(store)
            {
                InnerHandler = handler ?? new HttpClientHandler()
            });
        }
    }
}
