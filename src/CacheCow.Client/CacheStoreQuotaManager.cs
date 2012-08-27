using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CacheCow.Client
{
	public class CacheStoreQuotaManager
	{
		private CacheStoreSettings _settings;
		private ICacheMetadataProvider _metadataProvider;
		private Action<byte[]> _remover;
		private ConcurrentDictionary<string, long> _storageMetadata = new ConcurrentDictionary<string, long>();
		private object _lock = new object();
		private long _grandTotal = 0;
		private bool _doingHousekeeping = false;
		private bool _needsGrandTotalHouseKeeping = false;
		private bool _needsPerDomainHouseKeeping = false;

		public CacheStoreQuotaManager(ICacheMetadataProvider metadataProvider, Action<Byte[]> remover)
			: this(metadataProvider, remover, new CacheStoreSettings())
		{
			
		}

		public CacheStoreQuotaManager(ICacheMetadataProvider metadataProvider, Action<Byte[]> remover, 
			CacheStoreSettings settings)
		{
			_remover = remover;
			_metadataProvider = metadataProvider;
			_settings = settings;
			_needsGrandTotalHouseKeeping = settings.TotalQuota > 0;
			_needsPerDomainHouseKeeping = settings.PerDomainQuota > 0;
			BuildStorageMetadata();


		}

		public virtual void ItemAdded(CacheItemMetadata metadata)
		{
			_storageMetadata.AddOrUpdate(metadata.Domain, metadata.Size, (d, l) => l + metadata.Size);
			var total = _storageMetadata[metadata.Domain];
			lock (_lock)
			{
				_grandTotal += total;
			}

			if(_needsGrandTotalHouseKeeping && _grandTotal>_settings.TotalQuota)
				DoHouseKeepingAsync();

			if (_needsPerDomainHouseKeeping && total > _settings.PerDomainQuota)
				DoDomainHouseKeepingAsync(metadata.Domain);

		}

		public virtual void ItemRemoved(CacheItemMetadata metadata)
		{

			_storageMetadata.AddOrUpdate(metadata.Domain, metadata.Size, (d, l) => l - metadata.Size);
			var total = _storageMetadata[metadata.Domain];
			lock (_lock)
			{
				_grandTotal += total;
			}
		}

		private void BuildStorageMetadata()
		{
			_metadataProvider.GetDomains().ToList()
				.ForEach(domain =>
				         	{
				         		long domainSize = _metadataProvider.GetItemsMetadata(domain)
				         			.Sum(item => item.Size);
				         		_storageMetadata.AddOrUpdate(domain, domainSize, (s, l) => domainSize);
				         		lock (_lock)
				         		{
				         			_grandTotal += domainSize;
				         		}
				         	}
				);
		}

		private void DoHouseKeeping()
		{
			
		}

		private void DoHouseKeepingAsync()
		{
			Task.Factory.StartNew(DoHouseKeeping)
				.ContinueWith(t =>
				              	{
									if(t.IsFaulted)
				              			Trace.WriteLine(t.Exception);
				              	});
		}


		private void DoDomainHouseKeeping(object domain)
		{
			var dom = (string) domain;

		}


		private void DoDomainHouseKeepingAsync(string domain)
		{
			Task.Factory.StartNew(DoDomainHouseKeeping, (object) domain)
				.ContinueWith(t =>
				{
					if (t.IsFaulted)
						Trace.WriteLine(t.Exception);
				});

		}

	}
}
