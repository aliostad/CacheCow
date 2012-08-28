using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Common;
using System.Data.SQLite;
using Simple.Data;
using CacheCow.Common.Helpers;

namespace CacheCow.Client.FileCacheStore
{
	public class FileStore : ICacheStore, ICacheMetadataProvider
	{

		private MessageContentHttpMessageSerializer _serializer = new MessageContentHttpMessageSerializer();
		internal const string CacheMetadataDbName = "cache-metadata.db";
		private dynamic _database;
		private CacheStoreQuotaManager _quotaManager;
		private string _dataRoot;

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
			if(!Directory.Exists(dataRoot))
				Directory.CreateDirectory(dataRoot);

			string dbFile = Path.Combine(dataRoot, CacheMetadataDbName);
			if(!File.Exists(dbFile))
				BuildDb(dbFile);

			_database = Database.OpenFile(dbFile);
			_quotaManager = new CacheStoreQuotaManager(this, RemoveWithoutTellingQuotaManager);
		}

		private void BuildDb(string fileName)
		{
			using(var connection = new SQLiteConnection("data source=" + fileName))
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
			if(File.Exists(fileName))
			{
				using (var fs = new FileStream(fileName, FileMode.Create))
				{
					response = _serializer.DeserializeToResponseAsync(fs).Result;
				}

			}
			
			// TODO: update last access



			return response != null;
		}

		public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
			string fileName = key.EnsureFolderAndGetFileName(_dataRoot);
			if(File.Exists(fileName))
			{
				TryRemove(key);
			}
	
			using (var fs = new FileStream(fileName, FileMode.Create))
			{
				_serializer.SerializeAsync(TaskHelpers.FromResult(response), fs).Wait();
			}

			
			// TODO: Update database

			// tell quota manager
			_quotaManager.ItemAdded(new CacheItemMetadata()
			                        	{
			                        		Domain = key.Domain,
											Key = key.Hash,
											LastAccessed = DateTime.Now,
											Size = new FileInfo(fileName).Length
			                        	});
			
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
				_database.Cache.DeleteByHash(Convert.ToBase64String(metadata.Key));
				
				if(tellQuotaManager)
					_quotaManager.ItemRemoved(metadata);
				return true;
			}
			return false;

		}
	

		public void Clear()
		{
			_database.Cache.DeleteAll();
		}

		public IEnumerable<string> GetDomains()
		{
			 List<CacheItem> items = _database.Cache
				.All()
				.Select(_database.Cache.Domain)
				.Distinct()
				.ToList<CacheItem>();
			return items.Select(x => x.Domain);
		}

		public IEnumerable<CacheItemMetadata> GetItemsMetadata(string domain)
		{
			List<CacheItem> items = _database.Cache
				.FindAllByDomain(domain)
				.ToList<CacheItem>();
			return items.Select(x => x.Metadata);
		}

		public CacheItemMetadata GetLastAccessedItem(string domain)
		{
			return (CacheItemMetadata) _database.Cache				                       	
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
