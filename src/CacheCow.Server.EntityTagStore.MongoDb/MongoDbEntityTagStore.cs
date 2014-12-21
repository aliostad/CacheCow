namespace CacheCow.Server.EntityTagStore.MongoDb
{
	using System;
	using System.Configuration;
	using System.Linq;

	using CacheCow.Common;

	using MongoDB.Bson;
	using MongoDB.Driver.Builders;
	using MongoDB.Driver.Linq;

	public class MongoDbEntityTagStore : IEntityTagStore
	{
		private readonly string connectionString;

		private const string ConnectionStringName = "EntityTagStore";

		public MongoDbEntityTagStore()
		{
			if (!ConfigurationManager.ConnectionStrings.Cast<ConnectionStringSettings>()
				.Any(x => ConnectionStringName.Equals(x.Name, StringComparison.CurrentCultureIgnoreCase)))
			{
				throw new InvalidOperationException(
					string.Format(
						"Connection string with name '{0}' could not be found. Please create one or explicitly pass a connection string",
						ConnectionStringName));
			}

			this.connectionString = ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
		}

		public MongoDbEntityTagStore(string connectionString)
		{
			this.connectionString = connectionString;
		}
		
		public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
		{
			eTag = null;

			using (var connection = new MongoEntityStoreConnection(this.connectionString))
			{
				var cacheKey = connection.DocumentStore.AsQueryable().FirstOrDefault(x => x.Hash == key.Hash);
				if (null == cacheKey)
					return false;

				eTag = new TimedEntityTagHeaderValue(cacheKey.ETag)
				{
					LastModified = cacheKey.LastModified
				};

				return true;
			}
		}

		public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag)
		{
			TimedEntityTagHeaderValue test;
			if (!TryGetValue(key, out test))
			{
				var cacheKey = new PersistentCacheKey
				{
					Hash = key.Hash,
					RoutePattern = key.RoutePattern,
					ETag = eTag.Tag,
					LastModified = eTag.LastModified,
                    ResourceUri = key.ResourceUri
				};

				using (var connection = new MongoEntityStoreConnection(this.connectionString))
				{
					connection.DocumentStore.Save(cacheKey);
				}
			}
			else
			{
				using (var connection = new MongoEntityStoreConnection(this.connectionString))
				{
					var cacheKey = connection.DocumentStore.AsQueryable().FirstOrDefault(x => x.Hash == key.Hash);
					if (cacheKey != null)
					{
						cacheKey.ETag = eTag.Tag;
						cacheKey.LastModified = eTag.LastModified;
						connection.DocumentStore.Save(cacheKey);
					}
				}
			}
		}

	    public int RemoveResource(string resourceUri)
	    {
            int count;
            using (var connection = new MongoEntityStoreConnection(this.connectionString))
            {
                var persistentCacheKeys = connection.DocumentStore.AsQueryable()
                    .Where(p => p.ResourceUri == resourceUri);

                count = persistentCacheKeys.Count();

                foreach (var query in persistentCacheKeys
                    .Select(cacheKey => Query.EQ("_id", ObjectId.Parse(cacheKey.Id))))
                {
                    connection.DocumentStore.Remove(query);
                }
            }

            return count;
	    }

	    public bool TryRemove(CacheKey key)
		{
			int count;
			using (var connection = new MongoEntityStoreConnection(this.connectionString))
			{
				var persistentCacheKeys = connection.DocumentStore.AsQueryable().Where(p => p.Hash == key.Hash);
				count = persistentCacheKeys.Count();
				foreach (var query in persistentCacheKeys.Select(cacheKey => Query.EQ("_id", ObjectId.Parse(cacheKey.Id))))
				{
					connection.DocumentStore.Remove(query);
				}
			}

			return count > 0;
		}

		public int RemoveAllByRoutePattern(string routePattern)
		{
			int count;
			using (var connection = new MongoEntityStoreConnection(this.connectionString))
			{
				var persistentCacheKeys = connection.DocumentStore.AsQueryable().Where(p => p.RoutePattern == routePattern);

				count = persistentCacheKeys.Count();

				foreach (var query in persistentCacheKeys.Select(cacheKey => Query.EQ("_id", ObjectId.Parse(cacheKey.Id))))
				{
					connection.DocumentStore.Remove(query);
				}
			}

			return count;
		}

		public void Clear()
		{
			using (var connection = new MongoEntityStoreConnection(this.connectionString))
			{
				connection.DocumentStore.RemoveAll();
			}
		}

	    public void Dispose()
	    {
	        // nothing
	    }
	}
}
