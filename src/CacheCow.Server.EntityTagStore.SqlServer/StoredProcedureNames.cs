using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Server.EntityTagStore.SqlServer
{
    internal static class StoredProcedureNames
    {
        public static string GetCache = "Server_GetCache";
        public static string AddUpdateCache = "Server_AddUpdateCache";
        public static string DeleteCacheById = "Server_DeleteCacheById";
        public static string DeleteCacheByResourceUri = "Server_DeleteCacheByResourceUri";
        public static string DeleteCacheByRoutePattern = "Server_DeleteCacheByRoutePattern";
        public static string Clear = "Server_ClearCache";
    }
}