using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CacheCow.Common;
using NUnit.Framework;

namespace CacheCow.Server.EntityTagStore.SqlServer.Tests
{
	/// <summary>
	/// This class contains integration tests requiring sql server with an EntityTageStore database 
	/// As such all tests are ignored and must be run manually
	/// </summary>

	[TestFixture]
	public class IntegrationTests
	{
		[Test]
		[Ignore]
		public void AddTest()
		{
			var cacheKey = new CacheKey("/api/Cars", new[] {"1234", "abcdef"});
			var store = new SqlServerEntityTagStore();
			var value = new TimedEntityTagHeaderValue("\"abcdef1234\"") {LastModified = DateTime.Now};


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
			var cacheKey = new CacheKey("/api/Cars", new[] { "1234", "abcdef" });
			var store = new SqlServerEntityTagStore();
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
			var cacheKey = new CacheKey("/api/Cars", new[] { "1234", "abcdef" });
			var store = new SqlServerEntityTagStore();
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
			var cacheKey = new CacheKey("/api/Cars", new[] { "1234", "abcdef" });
			var cacheKey2 = new CacheKey("/api/Cars", new[] { "1234", "abcdefgh" });
			var store = new SqlServerEntityTagStore();
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
