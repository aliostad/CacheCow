using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace CacheCow.Server.ETagGeneration
{
    /// <summary>
    /// Default implementation of ETag generation. By default a weak ETag is generated
    /// </summary>
    public class DefaultETagGenerator : IETagGenerator
    {
        public virtual EntityTagHeaderValue Generate(string url, IEnumerable<KeyValuePair<string, IEnumerable<string>>> requestHeaders)
        {
            return new EntityTagHeaderValue(
                string.Format("\"{0}\"", Guid.NewGuid().ToString("N").ToLower()),
                false);

        }
    }
}
