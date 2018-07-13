using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CacheCow.Common.Helpers
{
	public static class HttpResponseMessageExtensions
	{
		public static Task<HttpResponseMessage> ToTask(this HttpResponseMessage responseMessage)
		{
			var taskCompletionSource = new TaskCompletionSource<HttpResponseMessage>();
			taskCompletionSource.SetResult(responseMessage);
			return taskCompletionSource.Task;
		}
		
		public static DateTimeOffset? GetExpiry(this HttpResponseMessage response)
		{      
			if (response.Headers.CacheControl != null && response.Headers.CacheControl.MaxAge.HasValue)
			{
				return DateTimeOffset.UtcNow.Add(response.Headers.CacheControl.MaxAge.Value);
			}
           
			return response.Content != null && response.Content.Headers.Expires.HasValue
				? response.Content.Headers.Expires.Value
				: (DateTimeOffset?) null;
		}
        
        public static async Task WhatEnsureSuccessShouldHaveBeen(this HttpResponseMessage response)
        {
            if(!response.IsSuccessStatusCode)
            {
                if(response.Content == null)
                {
                    response.EnsureSuccessStatusCode();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new HttpRequestException($"Status: {response.StatusCode}\r\n Error: {error}");
                }
            }
        }
	}
}
