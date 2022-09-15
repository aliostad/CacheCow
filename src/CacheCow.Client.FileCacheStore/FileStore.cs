using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// The directory location of the cache
        /// </summary>
        private readonly string _cacheRoot;

        /// <summary>
        /// Minimum expiry of items. Default is 6 hours.
        /// Bear in mind, even expired items can be used if we do a cache validation request and get back 304
        /// </summary>
        public TimeSpan MinExpiry { get; set; }

        private static readonly List<string> ForbiddenDirectories =
            new List<string>()
            {
                "/",
                "",
                ".",
                ".."
            };

        /// <summary>
        /// Create a new Cachestore within the given directory. Responses will be saved in this directory.
        /// The directory should not be "/", ".", "" or null.
        /// Note that _all_ contents of this directory can be cleared.
        /// </summary>
        /// <param name="cacheRoot">The directory containing the cache</param>
        /// <exception cref="ArgumentException">When the passed directory is "/", ".", ".." or ""</exception>
        public FileStore(string cacheRoot)
        {
            if (cacheRoot is null || ForbiddenDirectories.Contains(cacheRoot))
            {
                throw new ArgumentException(
                    "The given caching directory is null or invalid. Do give an explicit caching directory, not empty, '/' or '.'. This will prevent accidents when cleaning the cache");
            }

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

            using (var fs = File.OpenRead(_pathFor(key)))
            {
                return await _serializer.DeserializeToResponseAsync(fs);
            }
        }

        /// <inheritdoc />
        public async Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
        {
            using (var fs = File.Open(_pathFor(key), FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await _serializer.SerializeAsync(response, fs);
            }
        }

        /// <inheritdoc />
        public async Task<bool> TryRemoveAsync(CacheKey key)
        {
            if (!File.Exists(_pathFor(key)))
            {
                return false;
            }

            File.Delete(_pathFor(key));
            return true;
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
            // Base64 might return "/" as character. This breaks files; so we replace the '/' with '!'
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
            return Directory.GetFiles(_cacheRoot).Length == 0;
        }
    }
}
