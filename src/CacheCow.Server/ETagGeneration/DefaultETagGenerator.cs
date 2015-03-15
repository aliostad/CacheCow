using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;

namespace CacheCow.Server.ETagGeneration
{
    /// <summary>
    /// Default implementation of ETag generation. By default a weak ETag is generated
    /// </summary>
    public class DefaultETagGenerator : IETagGenerator
    {
        public virtual EntityTagHeaderValue Generate(HttpRequestMessage request, HttpConfiguration configuration)
        {
            return new EntityTagHeaderValue(
                string.Format("\"{0}\"", Guid.NewGuid().ToString("N").ToLower()),
                true); 

        }
    }
}
