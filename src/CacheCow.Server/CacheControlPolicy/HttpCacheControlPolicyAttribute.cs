using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace CacheCow.Server.CacheControlPolicy
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HttpCacheControlPolicyAttribute : Attribute
    {

        private readonly CacheControlHeaderValue _cacheControl;

        /// <summary>
        /// default .ctor is no cache policy
        /// </summary>
        public HttpCacheControlPolicyAttribute()
        {

            _cacheControl = new CacheControlHeaderValue()
            {
                Private = true,
                NoCache = true,
                NoStore = true
            };
        }


        public HttpCacheControlPolicyAttribute(bool isPrivate,
            int maxAgeInSeconds,
            bool mustRevalidate = true,
            bool noCache = false,
            bool noTransform = false,
            bool nostore = false)
            : this()
        {
            // copy values to the header
            _cacheControl = new CacheControlHeaderValue()
            {
                Private = isPrivate,
                Public = !isPrivate,
                MustRevalidate = mustRevalidate,
                MaxAge = TimeSpan.FromSeconds(maxAgeInSeconds),
                NoCache = noCache,
                NoTransform = noTransform,
                NoStore = nostore
            };
        }

        /// <summary>
        /// Uses a factory type to provide the value.
        /// This type can read from config, etc.
        /// Must have a public parameterless method that return CacheControlHeaderValue
        /// </summary>
        /// <param name="cacheControlHeaderValueFactory">The type of the factory. 
        /// Any public method that returns CacheControlHeaderValue will be used.
        /// Type's constructor must be parameterless</param>
        public HttpCacheControlPolicyAttribute(Type cacheControlHeaderValueFactory)
        {
            var factory = Activator.CreateInstance(cacheControlHeaderValueFactory);
            var method = factory.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.ReturnType == typeof(CacheControlHeaderValue));

            if (method == null)
                throw new ArgumentException("This type does not have a factory method: " + cacheControlHeaderValueFactory.FullName);

            _cacheControl = (CacheControlHeaderValue)method.Invoke(factory, new object[0]);

        }

        public CacheControlHeaderValue CacheControl
        {
            get { return _cacheControl; }
        }
    }
}
