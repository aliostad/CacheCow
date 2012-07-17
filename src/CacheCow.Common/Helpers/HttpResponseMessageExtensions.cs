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
	}
}
