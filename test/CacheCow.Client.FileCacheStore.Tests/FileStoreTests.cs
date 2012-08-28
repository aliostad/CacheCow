using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Simple.Data;

namespace CacheCow.Client.FileCacheStore.Tests
{
	[TestFixture]
	public class FileStoreTests
	{
		private string _dbFileName = Path.Combine(Path.GetTempPath(), FileStore.CacheMetadataDbName);
		private FileStore _store;

		[SetUp]
		public void Setup()
		{
			if (File.Exists(_dbFileName))
				File.Delete(_dbFileName);
			_store = new FileStore(Path.GetTempPath());			

		}

		[TearDown]
		public void TearDown()
		{
			//if (File.Exists(_dbFileName))
			//    File.Delete(_dbFileName);
		}

		[Test]
		public void Startup_No_File_Test()
		{
			Assert.IsTrue(File.Exists(_dbFileName));
		}

		[Test]
		public void Get_Last_Item_ByDomain_Test()
		{
			var database = Database.OpenFile(_dbFileName);
			var dateTime = DateTime.Now;
			database.Cache.Insert(new CacheItem()
			                      	{
			                      		Domain = "d",
										Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
										LastAccessed = dateTime,
										LastUpdated = dateTime,
										Size = 100										
			                      	});

			database.Cache.Insert(new CacheItem()
			{
				Domain = "d",
				Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
				LastAccessed = dateTime,
				LastUpdated = dateTime,
				Size = 100
			});


			var cacheItemMetadata = _store.GetLastAccessedItem("d");
			Assert.AreEqual(100, cacheItemMetadata.Size);
			Assert.AreEqual("d", cacheItemMetadata.Domain);
			Assert.AreEqual(dateTime, cacheItemMetadata.LastAccessed);
		}

		[Test]
		public void Get_Last_Item_Test()
		{
			var database = Database.OpenFile(_dbFileName);
			var dateTime = DateTime.Now;
			database.Cache.Insert(new CacheItem()
			{
				Domain = "c",
				Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
				LastAccessed = dateTime,
				LastUpdated = dateTime,
				Size = 50
			});

			database.Cache.Insert(new CacheItem()
			{
				Domain = "d",
				Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
				LastAccessed = dateTime.AddDays(-1),
				LastUpdated = dateTime.AddDays(-1),
				Size = 100
			});


			var cacheItemMetadata = _store.GetLastAccessedItem();
			Assert.AreEqual(100, cacheItemMetadata.Size);
			Assert.AreEqual("d", cacheItemMetadata.Domain);
			Assert.AreEqual(dateTime.AddDays(-1), cacheItemMetadata.LastAccessed);
		}

		[Test]
		public void Get_Domain_Test()
		{
			var database = Database.OpenFile(_dbFileName);
			var dateTime = DateTime.Now;
			database.Cache.Insert(new CacheItem()
			{
				Domain = "c",
				Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
				LastAccessed = dateTime,
				LastUpdated = dateTime,
				Size = 50
			});

			database.Cache.Insert(new CacheItem()
			{
				Domain = "d",
				Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
				LastAccessed = dateTime.AddDays(-1),
				LastUpdated = dateTime.AddDays(-1),
				Size = 100
			});

			var list = _store.GetDomains().ToList();
			Assert.AreEqual(2, list.Count);
			Assert.AreEqual(false, list[1] == "c" ^ list[0] == "d");

		}

		[Test]
		public void GetItemsMetadata_Test()
		{
			var database = Database.OpenFile(_dbFileName);
			var dateTime = DateTime.Now;
			database.Cache.Insert(new CacheItem()
			{
				Domain = "d",
				Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
				LastAccessed = dateTime,
				LastUpdated = dateTime,
				Size = 50
			});

			database.Cache.Insert(new CacheItem()
			{
				Domain = "d",
				Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
				LastAccessed = dateTime.AddDays(-1),
				LastUpdated = dateTime.AddDays(-1),
				Size = 100
			});

			var list = _store.GetItemsMetadata("d").ToList();
			Assert.AreEqual(2, list.Count);
			Assert.AreEqual(true, list[1].Domain == "d" && list[0].Domain == "d");

		}

		[Test]
		public void Clear_Test()
		{
			var database = Database.OpenFile(_dbFileName);
			var dateTime = DateTime.Now;
			database.Cache.Insert(new CacheItem()
			{
				Domain = "d",
				Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
				LastAccessed = dateTime,
				LastUpdated = dateTime,
				Size = 50
			});

			database.Cache.Insert(new CacheItem()
			{
				Domain = "d",
				Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
				LastAccessed = dateTime.AddDays(-1),
				LastUpdated = dateTime.AddDays(-1),
				Size = 100
			});

			_store.Clear();
			var list = _store.GetItemsMetadata("d").ToList();
			Assert.AreEqual(0, list.Count);

		}

	}
}
