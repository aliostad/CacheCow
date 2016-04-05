using System;
using System.Configuration;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;


namespace CacheCow.Server.CacheRefreshPolicy
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HttpCacheRefreshPolicyAttribute : Attribute
    {
        private readonly TimeSpan _refreshInterval;

        public HttpCacheRefreshPolicyAttribute(int refreshIntervalInSeconds)
        {
            _refreshInterval = TimeSpan.FromSeconds(refreshIntervalInSeconds);
        }

        /// <summary>
        /// Instantiates using a factory
        /// </summary>
        /// <param name="refreshTimeSpanFactory">Factory type. 
        /// Must have a public parameterless method returning a TimeSpan
        /// Type's constructor must be parameterless</param>
        public HttpCacheRefreshPolicyAttribute(Type refreshTimeSpanFactory)
        {
            var factory = Activator.CreateInstance(refreshTimeSpanFactory);
            var method = factory.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.ReturnType == typeof(TimeSpan));

            if (method == null)
                throw new ArgumentException("This type does not have a factory method: " + refreshTimeSpanFactory.FullName);

            _refreshInterval = (TimeSpan)method.Invoke(factory, new object[0]);

        }

        /// <summary>
        /// Allows to initialise the value from appSettings instead of hardcoding.
        /// Value must be an integer - number of seconds for interval
        /// </summary>
        /// <param name="appSettingsKeyName">Name of the key in the appSettings</param>
        public HttpCacheRefreshPolicyAttribute(string appSettingsKeyName)
        {
            var appSettingValue = ConfigurationManager.AppSettings[appSettingsKeyName];

            if (string.IsNullOrEmpty(appSettingValue))
                throw new InvalidOperationException("This appSettingsKeyName does not exist: " + appSettingsKeyName);

            int refreshSeconds;
            if(!int.TryParse(appSettingValue, out refreshSeconds))
                throw new FormatException("This appSettings value cannot be converted to int: " + appSettingValue);

            _refreshInterval = TimeSpan.FromSeconds(refreshSeconds);
        }

        public TimeSpan RefreshInterval
        {
            get { return _refreshInterval; }
        }
    }
}
