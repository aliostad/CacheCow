namespace CacheCow.Server
{
    using System;
    using System.Net.Http;

    public interface ICachingHandler
    {
        void InvalidateResources(HttpMethod method, params Uri[] resourceUris);
    }
}