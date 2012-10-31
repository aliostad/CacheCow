namespace CacheCow.Server.EntityTagStore.MongoDb.Tests
{
	using System;

	using CacheCow.Common;
	using CacheCow.Server.EntityTagStore.MongoDb.Tests.Embedded;

	using MongoDB.Driver;

	using NUnit.Framework;

	using Ninject;

	/// <summary>
	/// This class contains integration tests requiring mongo db with an EntityTagStore database 
	/// As such all tests are ignored and must be run manually
	/// </summary>

	[TestFixture]
	public class IntegrationTests
	{
		private IKernel kernel;

		private IMongoBootstrapper mongoBootstrapper;

		private MongoDatabase database;

		[TestFixtureSetUp]
		public void Setup()
		{
			this.kernel = new StandardKernel(new NinjectSettings());

			this.kernel.Bind<IMongoBootstrapper>().To<MongoBootstrapper>();
			this.kernel.Bind<IMongoDeployer>().To<MongoDeployer>();
			this.kernel.Bind<IResource>().To<Resource>();

			this.mongoBootstrapper = kernel.Get<IMongoBootstrapper>();
			this.mongoBootstrapper.Startup(MongoContextType.Test);
		}

		[TestFixtureTearDown]
		public void Teardown()
		{
			this.mongoBootstrapper.Shutdown();
			this.kernel.Dispose();
		}

		[Test]
		[Ignore]
		public void AddTest()
		{
			var store = new MongoDbEntityTagStore(this.mongoBootstrapper.Targets.ConnectionString);

			var cacheKey = new CacheKey("/api/Cars", new[] { "1234", "abcdef" });
			var value = new TimedEntityTagHeaderValue("\"abcdef1234\"") { LastModified = DateTime.Now };

			// first remove them
			store.RemoveAllByRoutePattern(cacheKey.RoutePattern);

			// add
			store.AddOrUpdate(cacheKey, value);

			// get
			TimedEntityTagHeaderValue dbValue;
			store.TryGetValue(cacheKey, out dbValue);


			Assert.AreEqual(value.Tag, dbValue.Tag);
			Assert.AreEqual(value.LastModified.ToString(), dbValue.LastModified.ToString());
		}

		[Test]
		[Ignore]
		public void UpdateTest()
		{
			var store = new MongoDbEntityTagStore(this.mongoBootstrapper.Targets.ConnectionString);

			var cacheKey = new CacheKey("/api/Cars", new[] { "1234", "abcdef" });
			
			var value = new TimedEntityTagHeaderValue("\"abcdef1234\"") { LastModified = DateTime.Now };
			
			// first remove them
			store.RemoveAllByRoutePattern(cacheKey.RoutePattern);

			// add
			store.AddOrUpdate(cacheKey, value);

			// get
			TimedEntityTagHeaderValue dbValue;
			store.TryGetValue(cacheKey, out dbValue);

			value.LastModified = DateTime.Now.AddDays(-1);

			// update
			store.AddOrUpdate(cacheKey, value);

			// get
			TimedEntityTagHeaderValue dbValue2;
			store.TryGetValue(cacheKey, out dbValue2);

			Assert.AreEqual(dbValue.Tag, dbValue2.Tag);
			Assert.Greater(dbValue.LastModified, dbValue2.LastModified);
			Console.WriteLine(dbValue2.Tag);
			Console.WriteLine(dbValue2.LastModified);
		}
		
		[Test]
		[Ignore]
		public void RemoveByIdTest()
		{
			var store = new MongoDbEntityTagStore(this.mongoBootstrapper.Targets.ConnectionString);

			var cacheKey = new CacheKey("/api/Cars", new[] { "1234", "abcdef" });
			var value = new TimedEntityTagHeaderValue("\"abcdef1234\"") { LastModified = DateTime.Now };
			
			// first remove them
			store.RemoveAllByRoutePattern(cacheKey.RoutePattern);

			// add
			store.AddOrUpdate(cacheKey, value);

			// delete
			Assert.True(store.TryRemove(cacheKey));
			Assert.True(!store.TryRemove(cacheKey));
		}

		[Test]
		[Ignore]
		public void RemoveByIdRoutePattern()
		{
			var store = new MongoDbEntityTagStore(this.mongoBootstrapper.Targets.ConnectionString);

			var cacheKey = new CacheKey("/api/Cars", new[] { "1234", "abcdef" });
			var cacheKey2 = new CacheKey("/api/Cars", new[] { "1234", "abcdefgh" });
			
			var value = new TimedEntityTagHeaderValue("\"abcdef1234\"") { LastModified = DateTime.Now };
			
			// first remove them
			store.RemoveAllByRoutePattern(cacheKey.RoutePattern);

			// add
			store.AddOrUpdate(cacheKey, value);
			store.AddOrUpdate(cacheKey2, value);

			// delete
			Assert.AreEqual(2, store.RemoveAllByRoutePattern("/api/Cars"));
			Assert.AreEqual(0, store.RemoveAllByRoutePattern("/api/Cars"));
		}
	}
}
