namespace CacheCow.Common
{
#if NET462

    using System.Configuration;

    /// <summary>
    ///
    /// </summary>
    public class ConfigurationValueProvider : IConfigurationValueProvider
    {
        /// <inheritdoc />
        public string GetValue(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }

#endif
    /// <summary>
    /// Abstraction on top of configuration
    /// </summary>
    public interface IConfigurationValueProvider
    {
        /// <summary>
        /// Returns a config value or null if it does not find the key
        /// </summary>
        /// <param name="key">the key</param>
        /// <returns>value</returns>
        string GetValue(string key);
    }
}
