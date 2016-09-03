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
using System.Threading.Tasks;

namespace CacheCow.Client.FileCacheStore
{
	public class FileStore : ICacheStore, ICacheMetadataProvider
	{
		private MessageContentHttpMessageSerializer _serializer = new MessageContentHttpMessageSerializer();
		internal const string CacheMetadataDbName = "cache-metadata.db";
		private const int ReaderWriterLockTimeout = 30000; // 30 seconds 
		private dynamic _database;
		private CacheStoreQuotaManager _quotaManager;
		private string _dataRoot;
		private string _dbFileName;
		private bool _bufferedFileAccess;

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
		// currently only buffered
		public FileStore(string dataRoot):this(dataRoot, true)
		{
					
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dataRoot"></param>
		/// <param name="bufferedFileAccess">It first loads the response into a memory buffer before processing. 
		/// Improves file locking especially if response takes long time to come back from server.
		/// </param>
		private FileStore(string dataRoot, bool bufferedFileAccess)
		{
			_bufferedFileAccess = bufferedFileAccess;
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


			string fileName = key.EnsureFolderAndGetFileName(_dataRoot);
			if (File.Exists(fileName))
			{
				var ms = new MemoryStream();
				using (var fs = GetFile(fileName, FileMode.Open))
				{
					TraceWriter.WriteLine("TryGetValue - before DeserializeToResponseAsync", TraceLevel.Verbose);
					fs.CopyTo(ms);
					ms.Position = 0;
				}
				response = _serializer.DeserializeToResponseAsync(ms).Result;
				TraceWriter.WriteLine("TryGetValue - After DeserializeToResponseAsync", TraceLevel.Verbose);
				if (response.Content != null)
				{
					var task = response.Content.LoadIntoBufferAsync();
					task.Wait();
					TraceWriter.WriteLine("TryGetValue - After  wait", TraceLevel.Verbose);
				}

				_database.Cache
					.UpdateByHash(new
					{
						Hash = Convert.ToBase64String(key.Hash),
						LastAccessed = DateTime.Now
					});

				TraceWriter.WriteLine("After updating Last Accessed", TraceLevel.Verbose);

			}


			return response != null;
		}

		public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
			string fileName = key.EnsureFolderAndGetFileName(_dataRoot);

			if (File.Exists(fileName))
			{
				TraceWriter.WriteLine("Must remove file", TraceLevel.Verbose);
				TryRemove(key);
			}

			var ms = new MemoryStream();
			_serializer.SerializeAsync(TaskHelpers.FromResult(response), ms).Wait();
			ms.Position = 0;
			using (var fs = GetFile(fileName, FileMode.Create))
			{
				TraceWriter.WriteLine("Before serialise", TraceLevel.Verbose);
				ms.CopyTo(fs);
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


		private static FileStream GetFile(string fileName, FileMode mode)
		{

			int retry = 0;
			int delay = 2000;

			while (retry < 3)
			{
				try
				{
					return new FileStream(fileName, mode);
				}
				catch (IOException e)
				{
					if (e.Message.Contains("process cannot access"))
					{
						Thread.Sleep(delay);
						retry++;
						delay *= 4;
					}
					else
						throw e;
				}
			}

			throw new TimeoutException("Could not access file after timeout and 3 retries: " + fileName);

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

		public CacheItemMetadata GetEarliestAccessedItem(string domain)
		{
			return (CacheItemMetadata)_database.Cache
				.FindAllByDomain(domain)
				.OrderBy(_database.Cache.LastAccessed)
				.Take(1)
				.FirstOrDefault();

		}

		public CacheItemMetadata GetEarliestAccessedItem()
		{

			return _database.Cache
				.All()
				.OrderBy(_database.Cache.LastAccessed)
				.Take(1)
				.FirstOrDefault();
		}

	    public void Dispose()
	    {
	        // none
	    }
	}
}
