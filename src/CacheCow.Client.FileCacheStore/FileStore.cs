using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Common;
using System.Data.SQLite;
using Simple.Data;

namespace CacheCow.Client.FileCacheStore
{
	public class FileStore : ICacheStore, ICacheMetadataProvider
	{

		private MessageContentHttpMessageSerializer _serializer = new MessageContentHttpMessageSerializer();
		internal const string CacheMetadataDbName = "cache-metadata.db";
		private dynamic _database;

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
			if(!Directory.Exists(dataRoot))
				Directory.CreateDirectory(dataRoot);

			string dbFile = Path.Combine(dataRoot, CacheMetadataDbName);
			if(!File.Exists(dbFile))
				BuildDb(dbFile);

			_database = Database.OpenFile(dbFile);
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
			throw new NotImplementedException();			
		}

		public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
			throw new NotImplementedException();
		}

		public bool TryRemove(CacheKey key)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<string> GetDomains()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<CacheItemMetadata> GetItemsMetadata(string domain)
		{
			throw new NotImplementedException();
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
