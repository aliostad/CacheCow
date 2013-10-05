namespace CacheCow.Server
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// This contains the only method server Caching has
    /// </summary>
    public interface ICachingHandler
    {
        /// <summary>
        /// Invalidates resources passed in
        /// All related to the same method
        /// </summary>
        /// <param name="method"></param>
        /// <param name="resourceUris"></param>
        void InvalidateResources(HttpMethod method, params Uri[] resourceUris);
    }
}