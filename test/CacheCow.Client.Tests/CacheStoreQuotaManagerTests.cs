using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CacheCow.Client;
using NUnit.Framework;
using Rhino.Mocks;

namespace CacheCow.Client.Tests
{
	[TestFixture]
	public class CacheStoreQuotaManagerTests
	{

		private CacheStoreQuotaManager _quotaManager;
		private MockRepository _mockRepository;
		private ICacheMetadataProvider _metadataProvider;
		private CacheStoreSettings _settings = new CacheStoreSettings(){ PerDomainQuota = 10, TotalQuota = 20};

		[SetUp]
		public void Setup()
		{
			_mockRepository = new MockRepository();
			_metadataProvider = _mockRepository.StrictMock<ICacheMetadataProvider>();
			_metadataProvider.Expect(x => x.GetDomainSizes()).Return(new Dictionary<string, long>());
		}

		[TearDown]
		public void TearDown()
		{
			_mockRepository.VerifyAll();
			_mockRepository = null;
		}

		[Test]
		public void Add_Test()
		{
			_mockRepository.ReplayAll();
			var manager = new CacheStoreQuotaManager(_metadataProvider, null, _settings);
			manager.ItemAdded(new CacheItemMetadata(){ Domain = "d", Size = 5});
			Assert.AreEqual(5, manager.GrandTotal);
			Assert.AreEqual(1, manager.StorageMetadata.Count);
		}

		[Test]
		public void Add_Two_Test_NotRemove()
		{
			bool called = false;
			_mockRepository.ReplayAll();
			var manager = new CacheStoreQuotaManager(_metadataProvider, (i) => called=true, _settings);
			manager.ItemAdded(new CacheItemMetadata(){ Domain = "d", Size = 5});
			manager.ItemAdded(new CacheItemMetadata(){ Domain = "d", Size = 5});
			Assert.AreEqual(false, called);
		}


		[Test]
		public void Add_Two_Test_Call_remove()
		{
			bool called = false;
			_metadataProvider.Expect(x => x.GetEarliestAccessedItem("d")).Return(new CacheItemMetadata()
			                                                                 	{
																					Domain = "d",
			                                                                 		Size = 5
			                                                                 	});
			_mockRepository.ReplayAll();

			var manager = new CacheStoreQuotaManager(_metadataProvider, (i) => called = true, _settings);
			manager.ItemAdded(new CacheItemMetadata() { Domain = "d", Size = 5});
			manager.ItemAdded(new CacheItemMetadata() { Domain = "d", Size = 6 });
			Thread.Sleep(1000);

			Assert.AreEqual(true, called);
			Assert.AreEqual(6, manager.GrandTotal);
			Assert.AreEqual(1, manager.StorageMetadata.Count);
		}

		[Test]
		public void Add_Test_GrandTotal_remove()
		{
			bool called = false;
			_metadataProvider.Expect(x => x.GetEarliestAccessedItem()).Return(new CacheItemMetadata()
			{
				Domain = "c",
				Size = 3
			});
			_mockRepository.ReplayAll();

			var manager = new CacheStoreQuotaManager(_metadataProvider, (i) => called = true, _settings);
			manager.ItemAdded(new CacheItemMetadata() { Domain = "a", Size = 9 });
			manager.ItemAdded(new CacheItemMetadata() { Domain = "b", Size = 9 });
			manager.ItemAdded(new CacheItemMetadata() { Domain = "c", Size = 3 });
			Thread.Sleep(1000);

			Assert.AreEqual(true, called);
			Assert.AreEqual(18, manager.GrandTotal);
			Assert.AreEqual(3, manager.StorageMetadata.Count);
		}


	}
}
