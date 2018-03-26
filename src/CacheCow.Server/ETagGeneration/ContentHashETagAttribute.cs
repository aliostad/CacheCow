using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http.Filters;

namespace CacheCow.Server.ETagGeneration
{
    /// <summary>
    /// Generates ETag based on MD5 hash of the content.
    /// </summary>
    public class ContentHashETagAttribute : ActionFilterAttribute
    {
        internal const string ContentHashHeaderName = "x-cachecow-content-hash-md5"; 
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);
            if(actionExecutedContext.Response.Content == null)
                return;

            var bytes = actionExecutedContext.Response.Content
                           .ReadAsByteArrayAsync().Result; // !!! Have to read as sync!!!

            using (var md5 = new MD5CryptoServiceProvider())
            {
                var hash = md5.ComputeHash(bytes);
                string hex = BitConverter.ToString(hash);
                hex = hex.Replace("-", "");
                actionExecutedContext.Request.Headers.TryAddWithoutValidation(ContentHashHeaderName, hex);
            }
        }       
    }
}
