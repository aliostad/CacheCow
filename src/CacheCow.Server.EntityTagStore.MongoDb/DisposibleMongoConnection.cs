namespace CacheCow.Server.EntityTagStore.MongoDb
{
	using System;

	using MongoDB.Driver;

	public class MongoEntiryStoreConnection : IDisposable
	{
		private readonly MongoServer server;

		private readonly MongoDatabase database;

		public MongoEntiryStoreConnection(string connectionString)
		{
            
			this.server = MongoServer.Create(connectionString);
            this.database = server.GetDatabase(new MongoUrl(connectionString).DatabaseName);
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
