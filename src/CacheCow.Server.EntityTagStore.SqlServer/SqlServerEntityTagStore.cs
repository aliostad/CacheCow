using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Common;

namespace CacheCow.Server.EntityTagStore.SqlServer
{
    /// <summary>
    /// Implements IEntityTagStore for SQL Server 2000 and above
    /// 
    /// Relies on a table and 4 store procedures (in scripts folder)
    /// 
    /// Uses plain ADO.NET. Not worth baking dependency for the sake of 5 calls.
    /// </summary>
    public class SqlServerEntityTagStore : IEntityTagStore
    {
        private readonly string _schema;
        private readonly string _connectionString;
        private const string ConnectionStringName = "EntityTagStore";
        private const string DefaultSchema = "dbo";

        public SqlServerEntityTagStore()
        {
            if (!ConfigurationManager.ConnectionStrings.Cast<ConnectionStringSettings>()
                .Any(x => ConnectionStringName.Equals(x.Name, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Connection string with name '{0}' could not be found. Please create one or explicitly pass a connection string",
                        ConnectionStringName));

            }

            this._schema = DefaultSchema;
            this._connectionString = ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
        }

        public SqlServerEntityTagStore(string connectionString)
            : this(connectionString, DefaultSchema) { }

        public SqlServerEntityTagStore(string connectionString, string schema)
        {
            this._schema = schema;
            this._connectionString = connectionString;
        }        

        /*********
		** Private methods
		*********/
        /// <summary>Prefixes a stored procedure name with the configured database schema name.</summary>
        /// <param name="procedure">The stored procedure name to format.</param>
        private string GetStoredProcedureName(string procedureName)
        {
            return String.Format("[{0}].[{1}]", this._schema, procedureName);
        }

        public void Dispose()
        {
            // nothing
        }

        public async Task<TimedEntityTagHeaderValue> GetValueAsync(CacheKey key)
        {
            using (var connection = new SqlConnection(this._connectionString))
            using (var command = new SqlCommand())
            {
                await connection.OpenAsync();
                command.Connection = connection;
                command.CommandText = this.GetStoredProcedureName(StoredProcedureNames.GetCache);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue(ColumnNames.CacheKeyHash, key.Hash);

                using (var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection))
                {
                    if (!reader.HasRows)
                        return null;

                    await reader.ReadAsync(); // there must be only one record
                    return new TimedEntityTagHeaderValue((string)reader[ColumnNames.ETag])
                    {
                        LastModified = DateTime.SpecifyKind((DateTime)reader[ColumnNames.LastModified], DateTimeKind.Utc)
                    };
                }
            }

        }

        public async Task AddOrUpdateAsync(CacheKey key, TimedEntityTagHeaderValue eTag)
        {
            using (var connection = new SqlConnection(this._connectionString))
            using (var command = new SqlCommand())
            {
                await connection.OpenAsync();
                command.Connection = connection;
                command.CommandText = this.GetStoredProcedureName(StoredProcedureNames.AddUpdateCache);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue(ColumnNames.CacheKeyHash, key.Hash);
                command.Parameters.AddWithValue(ColumnNames.RoutePattern, key.RoutePattern);
                command.Parameters.AddWithValue(ColumnNames.ResourceUri, key.ResourceUri);
                command.Parameters.AddWithValue(ColumnNames.ETag, eTag.Tag);
                command.Parameters.AddWithValue(ColumnNames.LastModified, eTag.LastModified.ToUniversalTime());
                await command.ExecuteNonQueryAsync();
            }

        }

        public async Task<int> RemoveResourceAsync(string resourceUri)
        {
            using (var connection = new SqlConnection(this._connectionString))
            using (var command = new SqlCommand())
            {
                await connection.OpenAsync();
                command.Connection = connection;
                command.CommandText = this.GetStoredProcedureName(StoredProcedureNames.DeleteCacheByResourceUri);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue(ColumnNames.ResourceUri, resourceUri);
                return await command.ExecuteNonQueryAsync();
            }

        }

        public async Task<bool> TryRemoveAsync(CacheKey key)
        {
            using (var connection = new SqlConnection(this._connectionString))
            using (var command = new SqlCommand())
            {
                await connection.OpenAsync();
                command.Connection = connection;
                command.CommandText = this.GetStoredProcedureName(StoredProcedureNames.DeleteCacheById);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue(ColumnNames.CacheKeyHash, key.Hash);
                return await command.ExecuteNonQueryAsync() > 0;
            }

        }

        public async Task<int> RemoveAllByRoutePatternAsync(string routePattern)
        {
            using (var connection = new SqlConnection(this._connectionString))
            using (var command = new SqlCommand())
            {
                await connection.OpenAsync();
                command.Connection = connection;
                command.CommandText = this.GetStoredProcedureName(StoredProcedureNames.DeleteCacheByRoutePattern);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue(ColumnNames.RoutePattern, routePattern);
                return await command.ExecuteNonQueryAsync();
            }

        }

        public async Task ClearAsync()
        {
            using (var connection = new SqlConnection(this._connectionString))
            using (var command = new SqlCommand())
            {
                await connection.OpenAsync();
                command.Connection = connection;
                command.CommandText = this.GetStoredProcedureName(StoredProcedureNames.Clear);
                command.CommandType = CommandType.StoredProcedure;
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}