using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server.Core
{
    /// <summary>
    /// Non-generic interface. Meant for internal use - please use the generic interface.
    /// </summary>
    public interface ITimedETagExtractor
    {
        TimedEntityTagHeaderValue Extract(object viewModel);
    }


    /// <summary>
    /// Extracts TETHV from a model/viewmodel - mainly when they do not implement ICacheResource
    /// </summary>
    /// <typeparam name="TViewModel">The view model.
    /// For example, the view model can be a collection of records (from database) with LastModified property and it extracts the latest LastModified from the collection
    /// </typeparam>
    public interface ITimedETagExtractor<TViewModel> : ITimedETagExtractor
    {
        /// <summary>
        /// Extracts from a view model
        /// </summary>
        /// <param name="t">view model</param>
        /// <returns>TETVH</returns>
        TimedEntityTagHeaderValue Extract(TViewModel t);
    }
}
