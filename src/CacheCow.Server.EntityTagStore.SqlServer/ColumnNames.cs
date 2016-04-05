using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Server.EntityTagStore.SqlServer
{
	internal class ColumnNames
	{
		public static string CacheKeyHash = "CacheKeyHash";
        public static string RoutePattern = "RoutePattern";
        public static string ResourceUri = "ResourceUri";
		public static string ETag = "ETag";
		public static string LastModified = "LastModified";
	}
}
