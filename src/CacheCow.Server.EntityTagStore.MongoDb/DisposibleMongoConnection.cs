using System;
using MongoDB.Driver;
namespace CacheCow.Server.EntityTagStore.MongoDb
{

	public class MongoEntityStoreConnection : IDisposable
	{
		private readonly MongoServer server;

		private readonly MongoDatabase database;

        public MongoEntityStoreConnection(string connectionString, string databaseName = "EntityTagStore")
		{
            this.server = new MongoClient(connectionString).GetServer();
            this.database = server.GetDatabase(databaseName);
		}

		public MongoCollection<PersistentCacheKey> DocumentStore
		{
			get
			{
				return this.database.GetCollection<PersistentCacheKey>("Keys");
			}
		}

		public MongoDatabase Database
		{
			get
			{
				return this.database;
			}
		}

		public void Dispose()
		{
			// After writing this I realised actually the c# driver takes care of everything for us. /Roysvork
		}
	}
}
