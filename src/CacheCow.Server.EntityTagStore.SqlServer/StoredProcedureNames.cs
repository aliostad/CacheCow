using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Server.EntityTagStore.SqlServer
{
	internal static class StoredProcedureNames
	{
		public static string GetCache = "GetCache";
		public static string AddUpdateCache = "AddUpdateCache";
		public static string DeleteCacheById = "DeleteCacheById";
		public static string DeleteCacheByRoutePattern = "DeleteCacheByRoutePattern";
	}
}
