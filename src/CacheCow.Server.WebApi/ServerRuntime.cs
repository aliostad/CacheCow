using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheCow.Server.WebApi
{
    /// <summary>
    /// Runtime environment for fine-tuning configuration of cache filters
    /// </summary>
    public static class ServerRuntime
    {
        public static event EventHandler<HttpCacheCreatedEventArgs> CacheFilterCreated;

        internal static void OnHttpCacheCreated(HttpCacheCreatedEventArgs args)
        {
            if (CacheFilterCreated != null)
                CacheFilterCreated(args.FilterInstance, args);
        }

        public static void RegisterFactory(Func<Type, object> factory, Action<Type, Type> registerationStub)
        {
            Factory = factory;

            AddTransient<ICacheabilityValidator, DefaultCacheabilityValidator>(registerationStub);
            AddTransient<HttpCacheAttribute, HttpCacheAttribute>(registerationStub);
            AddTransient<ISerialiser, JsonSerialiser>(registerationStub);
            AddTransient<IHasher, Sha1Hasher>(registerationStub);
            AddTransient<ITimedETagExtractor, DefaultTimedETagExtractor>(registerationStub);
            AddTransient<ITimedETagQueryProvider, NullQueryProvider>(registerationStub);
            AddTransient<ICacheDirectiveProvider, DefaultCacheDirectiveProvider>(registerationStub);
        }

        private static void AddTransient<TI, TC>(Action<Type, Type> registerationStub)
        {
            registerationStub(typeof(TI), typeof(TC));
        }

        internal static Func<Type, object> Factory { get; set; }

        internal static T Get<T>()
        {
            return (T)Factory(typeof(T));
        }
    }
}
