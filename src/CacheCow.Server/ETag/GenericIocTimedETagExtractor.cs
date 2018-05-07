using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server.ETag
{
    /// <summary>
    /// Allows an IoC factory drive dependency resolution of ITimedETagExtractor<ITimedETagExtractor>
    /// </summary>
    public class GenericIocTimedETagExtractor : ITimedETagExtractor
    {
        private readonly Func<Type, object> _factoryFromIoc;

        public GenericIocTimedETagExtractor(Func<Type, object> factoryFromIoc)
        {
            this._factoryFromIoc = factoryFromIoc;
        }

        public TimedEntityTagHeaderValue Extract(object viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException("videModel");

            var t = viewModel.GetType();

            if(t.IsGenericType && (t == typeof(Array) || t.GetGenericTypeDefinition() == typeof(List<>)))
            {
                t = typeof(IEnumerable<>).MakeGenericType(t.GenericTypeArguments[0]);
            }

            var genericType = typeof(ITimedETagExtractor<>).MakeGenericType(t);
            var extractor = (ITimedETagExtractor) _factoryFromIoc(genericType);
            return extractor.Extract(viewModel);
        }
    }
}
