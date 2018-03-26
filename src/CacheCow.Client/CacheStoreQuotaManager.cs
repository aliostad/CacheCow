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
		private Action<CacheItemMetadata> _remover;
		internal ConcurrentDictionary<string, long> StorageMetadata = new ConcurrentDictionary<string, long>();
		private object _lock = new object();
		internal long GrandTotal = 0;
		private bool _doingHousekeeping = false;
		private bool _needsGrandTotalHouseKeeping = false;
		private bool _needsPerDomainHouseKeeping = false;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="metadataProvider">Most likely implemented by the cache store itself</param>
		/// <param name="remover">This is a method most likely on the cache store which does not call
		/// back on ItemRemoved. This is very important.</param>
		public CacheStoreQuotaManager(ICacheMetadataProvider metadataProvider, Action<CacheItemMetadata> remover)
			: this(metadataProvider, remover, new CacheStoreSettings())
		{
			
		}

		public CacheStoreQuotaManager(ICacheMetadataProvider metadataProvider, Action<CacheItemMetadata> remover, 
			CacheStoreSettings settings)
		{
			_remover = remover;
			_metadataProvider = metadataProvider;
			_settings = settings;
			_needsGrandTotalHouseKeeping = settings.TotalQuota > 0;
			_needsPerDomainHouseKeeping = settings.PerDomainQuota > 0;
			BuildStorageMetadata();


		}

		public virtual void Clear()
		{
			StorageMetadata.Clear();
			GrandTotal = 0;
		}

		public virtual void ItemAdded(CacheItemMetadata metadata)
		{
			StorageMetadata.AddOrUpdate(metadata.Domain, metadata.Size, (d, l) => l + metadata.Size);
			var total = StorageMetadata[metadata.Domain];
			lock (_lock)
			{
				GrandTotal += metadata.Size;
			}

			if(_needsGrandTotalHouseKeeping && GrandTotal>_settings.TotalQuota)
				DoHouseKeepingAsync();

			if (_needsPerDomainHouseKeeping && total > _settings.PerDomainQuota)
				DoDomainHouseKeepingAsync(metadata.Domain);

		}

		public virtual void ItemRemoved(CacheItemMetadata metadata)
		{

			StorageMetadata.AddOrUpdate(metadata.Domain, metadata.Size, (d, l) => l - metadata.Size);
			lock (_lock)
			{
				GrandTotal -= metadata.Size;
			}
		}

		private void BuildStorageMetadata()
		{
			var domainSizes = _metadataProvider.GetDomainSizes();
			foreach (var domainSize in domainSizes)
			{
				lock (_lock)
				{
					GrandTotal += domainSize.Value;
				}
				StorageMetadata.AddOrUpdate(domainSize.Key, domainSize.Value, (k, v) => domainSize.Value);
			}
		}

		private void DoHouseKeeping()
		{
			while (GrandTotal > _settings.TotalQuota)
			{
				var item = _metadataProvider.GetEarliestAccessedItem();
				if(item!=null)
				{
					_remover(item);
					ItemRemoved(item);
				}
			}
			
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
			while (StorageMetadata[dom] > _settings.PerDomainQuota)
			{
				var item = _metadataProvider.GetEarliestAccessedItem(dom);
				if (item != null)
				{
					_remover(item);
					ItemRemoved(item);
				}
			}
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
