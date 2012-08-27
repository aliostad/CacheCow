using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheCow.Common;
using System.Data.SQLite;

namespace CacheCow.Client.FileCacheStore
{
	public class FileStore : ICacheStore
	{

		

		private MessageContentHttpMessageSerializer _serializer = new MessageContentHttpMessageSerializer();
		private const string CacheMetadataDbName = "";

		private const string CacheTableScript =
			@"CREATE TABLE `Cache`
(
       Hash TEXT,
       URL TEXT,
       Size INTEGER,
       LastUpdated DATETIME,
       PRIMARY KEY(Hash)
)";

		public FileStore(string dataRoot)
		{
			if(!Directory.Exists(dataRoot))
				Directory.CreateDirectory(dataRoot);

			string dbFile = Path.Combine(dataRoot, CacheMetadataDbName);
			if(!File.Exists(dbFile))
				SQLiteConnection.CreateFile(dbFile);
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
	}
}
