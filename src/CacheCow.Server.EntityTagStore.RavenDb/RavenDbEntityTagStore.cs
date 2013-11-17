using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using CacheCow.Common;
using Raven.Client;
using Raven.Client.Document;

namespace CacheCow.Server.EntityTagStore.RavenDb
{
	/// <summary>
	/// Implements IEntityTagStore for RavenDb
	/// </summary>
	public class RavenDbEntityTagStore : IEntityTagStore
	{
		readonly IDocumentStore _documentStore;
		private readonly string _connectionSting;
		private const string ConnectionStringName = "EntityTagStore";

		public RavenDbEntityTagStore()
		{
			if (!ConfigurationManager.ConnectionStrings.Cast<ConnectionStringSettings>()
				.Any(x => ConnectionStringName.Equals(x.Name, StringComparison.CurrentCultureIgnoreCase)))
			{
				throw new InvalidOperationException(
					string.Format(
						"Connection string with name '{0}' could not be found. Please create one or explicitly pass a connection string",
						ConnectionStringName));

			}

			_connectionSting = ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
			_documentStore = new DocumentStore()
			{
				ConnectionStringName = _connectionSting
			};
		}

		public RavenDbEntityTagStore(string connectionSting)
		{
			_connectionSting = connectionSting;
			_documentStore = new DocumentStore()
			{
				ConnectionStringName = _connectionSting
			};
		}

		public RavenDbEntityTagStore(IDocumentStore documentStore)
		{
			_documentStore = documentStore;
		}

		public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
		{
			eTag = null;
			using (var session = _documentStore.OpenSession())
			{
				var cacheKey = session.Query<PersistentCacheKey>()
					.Customize(x => x.WaitForNonStaleResultsAsOfNow())
					.FirstOrDefault(x => x.Hash == key.Hash);
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
				var cacheKey = new PersistentCacheKey()
				{
					Hash = key.Hash,
					RoutePattern = key.RoutePattern,
					ETag = eTag.Tag,
					LastModified = eTag.LastModified
				};
				using (var session = _documentStore.OpenSession())
				{
					session.Store(cacheKey);
					session.SaveChanges();
				}

			}
			else
			{
				using (var session = _documentStore.OpenSession())
				{
					var cacheKey =
						session.Query<PersistentCacheKey>()
						.Customize(x => x.WaitForNonStaleResults())
						.FirstOrDefault(x => x.Hash == key.Hash);
					cacheKey.ETag = eTag.Tag;
					cacheKey.LastModified = eTag.LastModified;
					session.Store(cacheKey);
					session.SaveChanges();
				}
			}
		}

	    public int RemoveResource(string url)
	    {
	        throw new NotImplementedException();
	    }

	    public bool TryRemove(CacheKey key)
		{
			var count = 0;
			using (var session = _documentStore.OpenSession())
			{
				var persistentCacheKeys =
					session.Query<PersistentCacheKey>()
					.Customize(x => x.WaitForNonStaleResults()).Where(p => p.Hash == key.Hash);
				count = persistentCacheKeys.Count();
				foreach (var cacheKey in persistentCacheKeys)
				{
					session.Delete(cacheKey);
				}
				session.SaveChanges();
			}
			return count > 0;
		}

		public int RemoveAllByRoutePattern(string routePattern)
		{
			var count = 0;
			using (var session = _documentStore.OpenSession())
			{
				var persistentCacheKeys =
					session.Query<PersistentCacheKey>()
					.Customize(x => x.WaitForNonStaleResults()).
					Where(p => p.RoutePattern == routePattern);

				count = persistentCacheKeys.Count();

				foreach (var key in persistentCacheKeys)
				{
					session.Delete(key);
				}

				session.SaveChanges();
			}
			return count;
		}

		public void Clear()
		{
			using (var connection = new SqlConnection(_connectionSting))
			using (var command = new SqlCommand())
			{
				connection.Open();
				command.Connection = connection;
				command.CommandText = "DELETE FROM db.CacheState;";
				command.CommandType = CommandType.Text;
				command.ExecuteNonQuery();
			}
		}


	    public void Dispose()
	    {
	        _documentStore.Dispose();
	    }
	}


}
