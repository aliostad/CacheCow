#if NET462
#else
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CacheCow.Server
{
    /// <summary>
    /// Main abstraction for caching in ASP.NET Core
    /// </summary>
    public interface ICachingPipeline
    {
        /// <summary>
        /// Whether in addition to sending cache directive for cacheable resources, it should send such directives for non-cachable resources
        /// </summary>
        bool ApplyNoCacheNoStoreForNonCacheableResponse { get; set; }

        /// <summary>
        /// Gets used to create Cache directives
        /// </summary>
        TimeSpan? ConfiguredExpiry { get; set; }

        /// <summary>
        /// To run after exection of action execution
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        Task After(HttpContext context, object viewModel);

        /// <summary>
        /// To be run before the action execution
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task<bool> Before(HttpContext context);

    }

    /// <summary>
    /// Generic type for ease of IoC
    /// </summary>
    /// <typeparam name="TViewModel"></typeparam>
    public interface ICachingPipeline<TViewModel> : ICachingPipeline
    {

    }
}
#endif
