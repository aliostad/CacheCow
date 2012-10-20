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
	/// 4) All LastAccessed
	/// Store as "SortedSet". negative of lastaccessed used as score.
	/// 
	/// 5) Domain-specific lastAccessed
	/// Store as "SortedSet". negative of lastaccessed used as score.
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
			public const string DomainSizePrefix = "String:CacheCow:Domain:Size";
			public const string DomainLastAccessedPrefix = "String:CacheCow:Domain:LastAccessed:";
			public const string LastAccessed = "String:CacheCow:LastAccessed";
			public const string AllEntries = "List:Entries";

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
				(int) (settings.IoTimeout <= TimeSpan.Zero ? -1 : settings.IoTimeout.TotalMilliseconds),
				string.IsNullOrEmpty(settings.Password) ? null : settings.Password,
				settings.MaxUnsentBytes,
				settings.AllowAdmin,
				(int) settings.SynTimeout.TotalMilliseconds);
			_databaseId = settings.DatabaseId;
		}

		public RedisStore(RedisConnection connection, int databaseId = 0, bool dispose = true)
		{
			_databaseId = databaseId;
			_dispose = dispose;
			_connection = connection;
		}

	
		public bool TryGetValue(CacheKey key, out HttpResponseMessage response)
		{
			HttpResponseMessage result = null;

			_connection.Hashes.Get(_databaseId, Fields.Response, key.Hash.ToBase64())
				.Then(bytes =>
				      	{
							var memoryStream = new MemoryStream(bytes);
				      		return _serializer.DeserializeToResponseAsync(memoryStream);
				      	})
						.Then(r => result = r)
						.Wait();

			response = result;
			return result != null;

		}

		public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
			var memoryStream = new MemoryStream();
			int length = 0;
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

			var uri = new Uri(key.ResourceUri);
			_connection.Strings.Increment(_databaseId, uri.Host, length)
				.Wait();

		}

		public bool TryRemove(CacheKey key)
		{
			return _connection.Keys.Remove(_databaseId, key.Hash.ToBase64())
				.Result;
		}

		public void Clear()
		{

			//_connection.Keys.
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

		public CacheItemMetadata GetLastAccessedItem(string domain)
		{
			var cacheItemMetadata = new CacheItemMetadata();
			cacheItemMetadata.Domain = domain;
			//cacheItemMetadata.
			var item = _connection.SortedSets.Range(_databaseId, 
				KeyNames.DomainLastAccessedPrefix + domain, 0,0).Result; // TODO!!

			if(item.Length==0)
				return null;
			string key = Encoding.UTF8.GetString(item[0].Key);
			cacheItemMetadata.Key = Convert.FromBase64String(key);
			var results = _connection.Hashes.GetValues(_databaseId, key).Result; // TODO!! refactor to do proper async
			cacheItemMetadata.Size = Convert.ToInt64(results[1]);
			cacheItemMetadata.LastAccessed = DateTime.Now; // TODO: this value is not really required. Decide whether deleted
			cacheItemMetadata.Domain = Encoding.UTF8.GetString(results[2]);
			return cacheItemMetadata;
		}

		public CacheItemMetadata GetLastAccessedItem()
		{
			var cacheItemMetadata = new CacheItemMetadata();
			//cacheItemMetadata.
			var item = _connection.SortedSets.Range(_databaseId,
				KeyNames.LastAccessed, 0, 0).Result; // TODO!! refactor to do proper async

			if (item.Length == 0)
				return null;
			string key = Encoding.UTF8.GetString(item[0].Key);
			cacheItemMetadata.Key = Convert.FromBase64String(key);
			var results = _connection.Hashes.GetValues(_databaseId, key).Result; // TODO!! refactor to do proper async
			cacheItemMetadata.Size = Convert.ToInt64(results[1]);
			cacheItemMetadata.LastAccessed = DateTime.Now; // TODO: this value is not really required. Decide whether deleted
			cacheItemMetadata.Domain = Encoding.UTF8.GetString(results[2]);
			return cacheItemMetadata;
		}

		#endregion
	}
}
