using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using Microsoft.Owin;

namespace UsingCacheCowWithNancyAndOwin
{
    internal static class OwinRequestResponseExtensions
    {
        public static HttpRequestMessage ToHttpRequestMessage(this IOwinRequest request)
        {
            var requestMessage = new HttpRequestMessage(new HttpMethod(request.Method), request.Uri);
            requestMessage.Content = new StreamContent(request.Body);
            request.Headers.ToList()
                .ForEach(x=> requestMessage.Headers.Add(x.Key, x.Value));            
            return requestMessage;
        }

        public static HttpResponseMessage ToHttpResponseMessage(this IOwinResponse response)
        {
            var requestMessage = response.Context.Request.ToHttpRequestMessage();
            var responseMessage = requestMessage.CreateResponse((HttpStatusCode) response.StatusCode);
            
            if(response.Body!=null)
                responseMessage.Content = new StreamContent(response.Body);
            
            foreach (var header in response.Headers)
            {
                if (!responseMessage.Headers.TryAddWithoutValidation(header.Key,
                                                                     header.Value))
                {
                    responseMessage.Content.Headers.TryAddWithoutValidation(
                        header.Key, header.Value);
                }
            }
            return responseMessage;
        }


    }
}