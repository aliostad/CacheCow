using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Client.RedisCacheStore.Helper;
using CacheCow.Common;
using CacheCow.Common.Helpers;
using BookSleeve;
using System.Threading;
using System.Threading.Tasks;

namespace CacheCow.Client.RedisCacheStore
{
	// TODO: 
	// 1) wrap all multiple operations in a transaction
	// 2) investigate streaming in Redis and change buffered solutions to streams if possible

	// --------------------------------------------------------------------------------------------------

	/// <summary>
	/// Stores the cache ina Redis instance
	/// 
	/// Storage
	/// =======
	/// 
	/// 1) Response
	/// stored as "Hash". Data (Response and its domain size) stored against a Base64 representation of CacheKey hash
	/// 
	/// 2) Domain Size
	/// Store as "String". Gets incremented/decremented
	/// 
	/// 3) Total Size
	/// Store as "String". Gets incremented/decremented
	/// 
	/// 4) All EarliestAccessed
	/// Store as "SortedSet". negative of EarliestAccessed used as score.
	/// 
	/// 5) Domain-specific EarliestAccessed
	/// Store as "SortedSet". negative of EarliestAccessed used as score.
	/// 
	/// 6) List of domains
	/// Stored as "Set".
	/// 
	/// 
	/// </summary>
	public class RedisStore : ICacheStore, ICacheMetadataProvider, IDisposable
	{

		private RedisConnection _connection;
		private bool _dispose;
		private int _databaseId;
		private MessageContentHttpMessageSerializer _serializer = new MessageContentHttpMessageSerializer();

		private class KeyNames
		{
			public const string DomainList = "List:CacheCow:Domains";
			public const string DomainPrefix = "String:CacheCow";
			public const string GlobalSize = "String:CacheCow:Size";
			public const string DomainSizePrefix = "String:CacheCow:Domain:Size";
			public const string DomainEarliestAccessedPrefix = "String:CacheCow:Domain:LastAccessed:";
			public const string EarliestAccessed = "String:CacheCow:EarliestAccessed";

		}

		private class Fields
		{
			public const string Size = "Size";
			public const string Response = "Response";
			public const string Domain = "Domain";
		}

		public RedisStore()
			: this(new RedisConnectionSettings())
		{

		}

		public RedisStore(string connectingString)
			: this(RedisConnectionSettings.Parse(connectingString))
		{

		}

		public RedisStore(RedisConnectionSettings settings)
		{
			_dispose = true;
			_connection = new RedisConnection(settings.HostName,
				settings.Port,
				(int)(settings.IoTimeout <= TimeSpan.Zero ? -1 : settings.IoTimeout.TotalMilliseconds),
				string.IsNullOrEmpty(settings.Password) ? null : settings.Password,
				settings.MaxUnsentBytes,
				settings.AllowAdmin,
				(int)settings.SynTimeout.TotalMilliseconds);
			_connection.Open();
			_databaseId = settings.DatabaseId;
		}

		public RedisStore(RedisConnection connection, int databaseId = 0, bool dispose = true)
		{
			_databaseId = databaseId;
			_dispose = dispose;
			_connection = connection;
		}

		/// <summary>
		/// Gets the value if exists
		/// ------------------------------------------
		/// 
		/// Steps:
		/// 
		/// 1) Get the value
		/// 2) Update domain-based earliest access
		/// 3) Update global earliest access
		/// </summary>
		/// <param name="key"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public bool TryGetValue(CacheKey key, out HttpResponseMessage response)
		{
			HttpResponseMessage result = null;
			response = null;
			string entryKey = key.Hash.ToBase64();
			if (!_connection.Keys.Exists(_databaseId, entryKey).Result)
				return false;

			_connection.Hashes.Get(_databaseId, entryKey, Fields.Response)
			.Then(bytes =>
			{
				var memoryStream = new MemoryStream(bytes);
				return _serializer.DeserializeToResponseAsync(memoryStream);
			})
					.Then(r => result = r)
					.Wait();

			response = result;
			bool resultBool = result != null;

			if (resultBool)
			{
				// step 2
				_connection.SortedSets.Add(_databaseId, KeyNames.DomainEarliestAccessedPrefix + key.Domain,
								  key.Hash, DateTime.Now.ToBinary()).Wait();

				// step 3
				_connection.SortedSets.Add(_databaseId, KeyNames.EarliestAccessed,
								  key.Hash, DateTime.Now.ToBinary()).Wait();

			}

			return resultBool;
		}


		/// <summary>
		/// Adds or updates an item in the store
		/// ------------------------------------
		/// 
		/// Steps:
		/// 1) Add the item
		/// 2) Update domain-specific sizes
		/// 3) Update total size
		/// 4) Add to last accessed in domain set
		/// 5) Add to general lasst accessed set 
		/// 6) Add domain (if not exists)
		/// </summary>
		/// <param name="key"></param>
		/// <param name="response"></param>
		public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
			var memoryStream = new MemoryStream();
			int length = 0;

			// step 1
			_serializer.SerializeAsync(response.ToTask(), memoryStream)
				.Then(() =>
				{
					var bytes = memoryStream.ToArray();
					length = bytes.Length;
					var dictionary = new Dictionary<string, byte[]>();
					dictionary.Add(Fields.Response, bytes);
					dictionary.Add(Fields.Size, BitConverter.GetBytes(length));
					dictionary.Add(Fields.Domain, Encoding.UTF8.GetBytes(key.Domain));

					return _connection.Hashes.Set(_databaseId,
						key.Hash.ToBase64(), dictionary);
				})
				.Wait();

			// setp 2
			_connection.Strings.Increment(_databaseId, KeyNames.DomainPrefix + key.Domain, length)
				.Wait();

			// step 3
			_connection.Strings.Increment(_databaseId, KeyNames.GlobalSize, length)
				.Wait();

			// setp 4
			_connection.SortedSets.Add(_databaseId, KeyNames.DomainEarliestAccessedPrefix + key.Domain,
							  key.Hash, DateTime.Now.ToBinary()).Wait();

			// step 5
			_connection.SortedSets.Add(_databaseId, KeyNames.EarliestAccessed,
							  key.Hash, DateTime.Now.ToBinary()).Wait();

			// setp 6
			_connection.Sets.Add(_databaseId, KeyNames.DomainList, key.Domain);

		}

		/// <summary>
		/// Removes item in cache by its key
		/// 
		/// Steps:
		/// 
		/// 1) Remove the item
		/// 2) Update domain-specific sizes
		/// 3) Update total size
		/// 4) Remove from last accessed in domain set
		/// 5) Remove from general lasst accessed set
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool TryRemove(CacheKey key)
		{
			if (!_connection.Keys.Exists(_databaseId, key.Hash.ToBase64()).Result)
				return false;

			var sizeBytes = _connection.Hashes.Get(_databaseId, key.Hash.ToBase64(), Fields.Size).Result;
			long size = BitConverter.ToInt64(sizeBytes, 0);

			// setp 1
			bool result = _connection.Keys.Remove(_databaseId, key.Hash.ToBase64())
				.Result;

			if (result)
			{
				// step 2
				_connection.Strings.Decrement(_databaseId, KeyNames.DomainSizePrefix + key.Domain,
											  size).Wait();

				// setp 3
				_connection.Strings.Decrement(_databaseId, KeyNames.GlobalSize,
											  size).Wait();

				// step 4
				_connection.SortedSets.Remove(_databaseId, KeyNames.DomainEarliestAccessedPrefix + key.Domain,
											  key.Hash).Wait();

				// step 5
				_connection.SortedSets.Remove(_databaseId, KeyNames.EarliestAccessed,
											  key.Hash).Wait();

			}
			return result;
		}

		public void Clear()
		{
			throw new NotSupportedException("Currently not supported. Might get implemented later"); // TODO: think of implementation 
		}

		public void Dispose()
		{
			if (_connection != null && _dispose)
				_connection.Dispose();
		}

		#region Implementation of ICacheMetadataProvider

		public IDictionary<string, long> GetDomainSizes()
		{
			Dictionary<string, long> sizes = new Dictionary<string, long>();
			_connection.Lists.Range(_databaseId, KeyNames.DomainList, 0, int.MaxValue)
				.ContinueWith(t =>
				{
					var result = t.Result;
					for (int i = 0; i < result.Length; i++)
					{
						string domain = Encoding.UTF8.GetString(result[i]);
						var size = _connection.Strings.Get(_databaseId, KeyNames.DomainSizePrefix + domain).Result;
						sizes.Add(domain, BitConverter.ToInt64(size, 0));
					}

				}).Wait();
			return sizes;
		}

		public CacheItemMetadata GetEarliestAccessedItem(string domain)
		{
			var cacheItemMetadata = new CacheItemMetadata();
			cacheItemMetadata.Domain = domain;
			//cacheItemMetadata.
			var item = _connection.SortedSets.Range(_databaseId,
				KeyNames.DomainEarliestAccessedPrefix + domain, 0, 0).Result; // TODO!!

			if (item.Length == 0)
				return null;

			cacheItemMetadata.Key = item[0].Key;
			var results = _connection.Hashes.GetValues(_databaseId, item[0].Key.ToBase64()).Result; // TODO!! refactor to do proper async
			cacheItemMetadata.Size = BitConverter.ToInt64(results[1], 0);
			cacheItemMetadata.LastAccessed = DateTime.Now; // TODO: this value is not really required. Decide whether deleted
			cacheItemMetadata.Domain = Encoding.UTF8.GetString(results[2]);
			return cacheItemMetadata;
		}

		public CacheItemMetadata GetEarliestAccessedItem()
		{
			var cacheItemMetadata = new CacheItemMetadata();
			var item = _connection.SortedSets.Range(_databaseId,
				KeyNames.EarliestAccessed, 0, 0).Result; // TODO!! refactor to do proper async

			if (item.Length == 0)
				return null;
			cacheItemMetadata.Key = item[0].Key;
			var results = _connection.Hashes.GetValues(_databaseId, item[0].Key.ToBase64()).Result; // TODO!! refactor to do proper async
			cacheItemMetadata.Size = BitConverter.ToInt64(results[1], 0);
			cacheItemMetadata.LastAccessed = DateTime.Now; // TODO: this value is not really required. Decide whether deleted
			cacheItemMetadata.Domain = Encoding.UTF8.GetString(results[2]);
			return cacheItemMetadata;
		}

		#endregion
	}
}
