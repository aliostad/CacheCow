namespace CacheCow.Server.EntityTagStore.MongoDb
{
	using System;

	using MongoDB.Bson;
	using MongoDB.Bson.Serialization.Attributes;

	public class PersistentCacheKey
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }

		public byte[] Hash { get; set; }

		public string RoutePattern { get; set; }

		public string ETag { get; set; }

		public DateTimeOffset LastModified { get; set; }
	}
}
