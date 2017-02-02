using System;
using System.Collections.Generic;
using System.Linq;
using CacheCow.Common;
using Nest;

namespace CacheCow.Server.EntityTagStore.Elasticsearch
{
    public class NestEntityTagStore : IEntityTagStore
	{
		private readonly ElasticClient _elasticsearchClient;
        private const string ElasticsearchIndex = "cachecow";

        public NestEntityTagStore(string elasticsearchUrl)
        {
            var uri = new Uri(elasticsearchUrl);
            var settings = new ConnectionSettings(uri).DefaultIndex(ElasticsearchIndex);
            _elasticsearchClient = new ElasticClient(settings);
        }

        private PersistentCacheKey TryGetPersistentCacheKey(string key)
        {
            var idsList = new List<string> { key };
            var result = _elasticsearchClient.Search<PersistentCacheKey>(s => s
                .Index(ElasticsearchIndex)
                .AllTypes()
                .Query(p => p.Ids(d => d.Values(idsList))));

            if (result.Documents.Any())
            {
                return result.Documents.First();
            }

            return null;
        }

        public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
        {
            eTag = null;
            var persistentCacheKey = TryGetPersistentCacheKey(key.HashBase64);
            if (persistentCacheKey != null)
            {
                eTag = new TimedEntityTagHeaderValue(persistentCacheKey.ETag)
                {
                    LastModified = persistentCacheKey.LastModified
                };

                return true; 
            }
                
            return false;
        }

        public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag)
        {
            var cacheKey = TryGetPersistentCacheKey(key.HashBase64);
            if (cacheKey != null)
            {
                // update existing
                cacheKey.ETag = eTag.Tag;
                cacheKey.LastModified = eTag.LastModified;
            }
            else
            {
                // Create new
                cacheKey = new PersistentCacheKey
                {
                    Id = key.HashBase64,
                    RoutePattern = key.RoutePattern,
                    ETag = eTag.Tag,
                    LastModified = eTag.LastModified,
                    ResourceUri = key.ResourceUri
                };
            }
            _elasticsearchClient.Index(cacheKey, d => d.Index(ElasticsearchIndex));
        }

        public int RemoveResource(string resourceUri)
        {
            var items = resourceUri.Trim('/').Split('/');

            var result = _elasticsearchClient.Search<PersistentCacheKey>(s => s.Index(ElasticsearchIndex)
                .AllTypes()
                .Query(q => q.Terms(tq => tq
                    .Field(f => f.ResourceUri)
                    .Terms(items)
                    )
                ));

            int count = result.Documents.Count();

            foreach (var item in result.Documents)
            {
                _elasticsearchClient.Delete(new DeleteRequest(ElasticsearchIndex, ElasticsearchIndex, item.Id));
            }

            return count;
        }

        public bool TryRemove(CacheKey key)
        {
            var idsList = new List<string> { key.HashBase64 };
            var result = _elasticsearchClient.Search<PersistentCacheKey>(s => s
                .Index(ElasticsearchIndex)
                .AllTypes()
                .Query(p => p.Ids(d => d.Values(idsList))));

            int count = result.Documents.Count();

            foreach (var item in result.Documents)
            {
                _elasticsearchClient.Delete(new DeleteRequest(ElasticsearchIndex, ElasticsearchIndex, item.Id));
            }

            return count > 0;
        }

        public int RemoveAllByRoutePattern(string routePattern)
        {
            var items = routePattern.Trim('+').Trim('/').Split('/');

            var result = _elasticsearchClient.Search<PersistentCacheKey>(s => s.Index(ElasticsearchIndex)
                .AllTypes()
                .Query(q => q.Terms(tq => tq
                    .Field(f => f.ResourceUri)
                    .Terms(items)
                    )
                ));

            int count = result.Documents.Count();

            foreach (var item in result.Documents)
            {
                _elasticsearchClient.Delete(new DeleteRequest(ElasticsearchIndex, ElasticsearchIndex, item.Id));
            }

            return count;
        }

        public void Clear()
        {
            _elasticsearchClient.DeleteIndex(ElasticsearchIndex);
        }

        public void Dispose()
        {

        }
	}
}
