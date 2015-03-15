using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace CacheCow.Server.ETagGeneration
{
    /// <summary>
    /// 
    /// </summary>
    public interface IETagGenerator
    {
        EntityTagHeaderValue Generate(HttpRequestMessage request, HttpConfiguration configuration);
    }
}
