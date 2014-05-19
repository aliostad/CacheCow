using System;

namespace CacheCow.Server.EntityTagStore.Elasticsearch
{
    public class PersistentCacheKey
	{
        public const string SearchIndex = "cachecow";

		public string Id { get; set; }

        public string RoutePattern { get; set; }
        
        public string ResourceUri { get; set; }

		public string ETag { get; set; }

		public DateTimeOffset LastModified { get; set; }
	}
}
