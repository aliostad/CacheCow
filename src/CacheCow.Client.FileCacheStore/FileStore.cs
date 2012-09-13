using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using CacheCow.Common;
using System.Data.SQLite;
using Simple.Data;
using CacheCow.Common.Helpers;

namespace CacheCow.Client.FileCacheStore
{
	public class FileStore : ICacheStore, ICacheMetadataProvider
	{
		private ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();
		private MessageContentHttpMessageSerializer _serializer = new MessageContentHttpMessageSerializer();
		internal const string CacheMetadataDbName = "cache-metadata.db";
		private const int ReaderWriterLockTimeout = 30000; // 30 seconds 
		private dynamic _database;
		private CacheStoreQuotaManager _quotaManager;
		private string _dataRoot;
		private string _dbFileName;

		private const string CacheTableScript =
			@"CREATE TABLE `Cache`
(
       Hash TEXT,
       Domain TEXT,
       Size INTEGER,
       LastUpdated DATETIME,
       LastAccessed DATETIME,
       PRIMARY KEY(Hash)
)";

		public FileStore(string dataRoot)
		{
			_dataRoot = dataRoot;
			if (!Directory.Exists(dataRoot))
				Directory.CreateDirectory(dataRoot);

			_dbFileName = Path.Combine(dataRoot, CacheMetadataDbName);
			if (!File.Exists(_dbFileName))
				BuildDb(_dbFileName);

			_database = Database.OpenFile(_dbFileName);
			_quotaManager = new CacheStoreQuotaManager(this, RemoveWithoutTellingQuotaManager);
		}

		private void BuildDb(string fileName)
		{
			using (var connection = new SQLiteConnection("data source=" + fileName))
			{
				connection.Open();
				var command = connection.CreateCommand();
				command.CommandText = CacheTableScript;
				command.ExecuteNonQuery();
			}
		}

		public bool TryGetValue(CacheKey key, out HttpResponseMessage response)
		{
			response = null;

			bool lockAttained = false;
			try
			{
				lockAttained = _lockSlim.TryEnterReadLock(ReaderWriterLockTimeout);
				if (!lockAttained)
					return false;

				string fileName = key.EnsureFolderAndGetFileName(_dataRoot);
				if (File.Exists(fileName))
				{


					using (var fs = new FileStream(fileName, FileMode.Open))
					{
						TraceWriter.WriteLine("TryGetValue - before DeserializeToResponseAsync", TraceLevel.Verbose);
						response = _serializer.DeserializeToResponseAsync(fs).Result;
						TraceWriter.WriteLine("TryGetValue - After DeserializeToResponseAsync", TraceLevel.Verbose);
						if (response.Content != null)
						{
							var task = response.Content.LoadIntoBufferAsync();
							task.Wait();

							TraceWriter.WriteLine("TryGetValue - After  wait", TraceLevel.Verbose);
						}
					}

					_database.Cache
						.UpdateByHash(new
						{
							Hash = Convert.ToBase64String(key.Hash),
							LastAccessed = DateTime.Now
						});

					TraceWriter.WriteLine("After updating Last Accessed", TraceLevel.Verbose);



				}
			}
			finally
			{
				if (lockAttained)
					_lockSlim.ExitReadLock();
			}


			return response != null;
		}

		public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
			bool lockAttained = false;
			try
			{
				
				lockAttained = _lockSlim.TryEnterWriteLock(ReaderWriterLockTimeout);

				TraceWriter.WriteLine("Start - lockAttained: {0}", TraceLevel.Verbose, lockAttained);

				if (!lockAttained)
					return;

				string fileName = key.EnsureFolderAndGetFileName(_dataRoot);

				if (File.Exists(fileName))
				{
					TraceWriter.WriteLine("Must remove file", TraceLevel.Verbose);

					TryRemove(key);
				}


				using (var fs = new FileStream(fileName, FileMode.Create))
				{
					TraceWriter.WriteLine("Before serialise", TraceLevel.Verbose);
					_serializer.SerializeAsync(TaskHelpers.FromResult(response), fs).Wait();
					TraceWriter.WriteLine("After serialise", TraceLevel.Verbose);
				}
				var info = new FileInfo(fileName);

				// Update database
				_database.Cache
					.Insert(new CacheItem()
					{
						Domain = key.Domain,
						Hash = Convert.ToBase64String(key.Hash),
						LastAccessed = DateTime.Now,
						LastUpdated = response.Content != null && response.Content.Headers.LastModified.HasValue ?
							response.Content.Headers.LastModified.Value.UtcDateTime : DateTime.Now
						,
						Size = info.Length
					});
				TraceWriter.WriteLine("After db update", TraceLevel.Verbose);


				// tell quota manager
				_quotaManager.ItemAdded(new CacheItemMetadata()
				{
					Domain = key.Domain,
					Key = key.Hash,
					LastAccessed = DateTime.Now,
					Size = info.Length
				});

			}
			finally
			{
				if (lockAttained)
					_lockSlim.ExitWriteLock();
			}

		}

		public bool TryRemove(CacheKey key)
		{
			return TryRemove(new CacheItemMetadata()
			{
				Domain = key.Domain,
				Key = key.Hash
			}, true);
		}

		private void RemoveWithoutTellingQuotaManager(CacheItemMetadata metadata)
		{
			TryRemove(metadata, false);
		}

		private bool TryRemove(CacheItemMetadata metadata, bool tellQuotaManager)
		{
			bool lockAttained = true;
			try
			{
				TraceWriter.WriteLine("Attempting lock: {0}", TraceLevel.Verbose, lockAttained);
				lockAttained = _lockSlim.TryEnterWriteLock(ReaderWriterLockTimeout);
				TraceWriter.WriteLine("lockAttained: {0}", TraceLevel.Verbose, lockAttained);
				if (!lockAttained)
					return false;
				var fileName = metadata.EnsureFolderAndGetFileName(_dataRoot);
				if (File.Exists(fileName))
				{
					File.Delete(fileName);
					TraceWriter.WriteLine("After delete file {0}", TraceLevel.Verbose, fileName);

					_database.Cache.DeleteByHash(Convert.ToBase64String(metadata.Key));

					TraceWriter.WriteLine("After db update. File name: {0}", TraceLevel.Verbose, fileName);

					if (tellQuotaManager)
						_quotaManager.ItemRemoved(metadata);
					return true;

				}
			}
			finally
			{
				if (lockAttained)
					_lockSlim.ExitWriteLock();
			}

			return false;

		}


		public void Clear()
		{
			_database.Cache.DeleteAll();
		}

		public IDictionary<string, long> GetDomainSizes()
		{
			var dictionary = new Dictionary<string, long>();
			using (var cn = new SQLiteConnection("data source=" + _dbFileName))
			{
				cn.Open();
				var cm = cn.CreateCommand();
				cm.CommandType = CommandType.Text;
				cm.CommandText = "SELECT Domain, SUM(Size) FROM CACHE GROUP BY Domain";
				var reader = cm.ExecuteReader();
				while (reader.Read())
				{
					dictionary.Add((string)reader[0], (long)reader[1]);
				}
			}

			return dictionary;
		}

		public CacheItemMetadata GetLastAccessedItem(string domain)
		{
			return (CacheItemMetadata)_database.Cache
				.FindAllByDomain(domain)
				.OrderBy(_database.Cache.LastAccessed)
				.Take(1)
				.FirstOrDefault();

		}

		public CacheItemMetadata GetLastAccessedItem()
		{

			return _database.Cache
				.All()
				.OrderBy(_database.Cache.LastAccessed)
				.Take(1)
				.FirstOrDefault();
		}
	}
}
