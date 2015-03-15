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
    /// Generates ETag based where ContentHashETagAttribute has been defined on action 
    /// </summary>
    public class ContentHashETagGenerator : DefaultETagGenerator
    {
        public override EntityTagHeaderValue Generate(HttpRequestMessage request, HttpConfiguration configuration)
        {
            object hash;

            if (request.Properties.TryGetValue(ContentHashETagAttribute.ContentHashPropertyName, out hash))
            {
                return new EntityTagHeaderValue("\"" + hash + "\"", false);
            }

            return base.Generate(request, configuration);
        }
    }
}
