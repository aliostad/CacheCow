using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CacheCow.Common;

namespace CacheCow.Client.FileCacheStore
{
    /// <summary>
    /// A simple 'cache-to-file' storage with persistanty over multiple runs.
    /// </summary>
    public class FileStore : ICacheStore
    {
        private readonly MessageContentHttpMessageSerializer _serializer = new MessageContentHttpMessageSerializer();

        private readonly string _cacheRoot;

        /// <summary>
        /// Minimum expiry of items. Default is 6 hours.
        /// Bear in mind, even expired items can be used if we do a cache validation request and get back 304
        /// </summary>
        public TimeSpan MinExpiry { get; set; }


        /// <inheritdoc />
        public FileStore(string cacheRoot)
        {
            _cacheRoot = cacheRoot;
            if (!Directory.Exists(_cacheRoot))
            {
                Directory.CreateDirectory(cacheRoot);
            }
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetValueAsync(CacheKey key)
        {
            if (!File.Exists(_pathFor(key)))
            {
                return null;
            }

            var fs = File.OpenRead(_pathFor(key));
            var resp = await _serializer.DeserializeToResponseAsync(fs);
            fs.Close();
            return resp;
        }

        /// <inheritdoc />
        public async Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
        {
            var fs = File.OpenWrite(_pathFor(key));
            await _serializer.SerializeAsync(response, fs);
            fs.Close();
        }

        /// <inheritdoc />
        public Task<bool> TryRemoveAsync(CacheKey key)
        {
            return new Task<bool>(() =>
                {
                    if (!File.Exists(_pathFor(key)))

                    {
                        return false;
                    }

                    File.Delete(_pathFor(key));
                    return true;
                }
            );
        }

        /// <inheritdoc />
        public async Task ClearAsync()
        {
            foreach (var f in Directory.GetFiles(_cacheRoot))
            {
               File.Delete(f);
            }
        }


        private string _pathFor(CacheKey key)
        {
            // Who the fuck thought using '/' in a Base64-encoding was a good idea?
            return _cacheRoot + "/" + key.HashBase64.Replace('/', '!');
        }


        /// <inheritdoc />
        public void Dispose()
        {
            // Nothing to do here
        }

        /// <summary>
        /// Checks if the cache is empty
        /// </summary>
        /// <returns>True if no files are in the current cache</returns>
        public bool IsEmpty()
        {
            return Directory.GetFiles(_cacheRoot).Length==0;
        }
    }
}
