using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
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
			if(!ConfigurationManager.ConnectionStrings.Cast<ConnectionStringSettings>()
				.Any(x=>ConnectionStringName.Equals(x.Name, StringComparison.CurrentCultureIgnoreCase)))
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
			: this(connectionString, DefaultSchema) {}

		public SqlServerEntityTagStore(string connectionString, string schema)
		{
			this._schema = schema;
			this._connectionString = connectionString;
		}

		public bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag)
		{
			eTag = null;
			using (var connection = new SqlConnection(this._connectionString))
			using (var command = new SqlCommand())
			{
				connection.Open();
				command.Connection = connection;
				command.CommandText = this.GetStoredProcedureName(StoredProcedureNames.GetCache);
				command.CommandType = CommandType.StoredProcedure;
				command.Parameters.AddWithValue(ColumnNames.CacheKeyHash, key.Hash);

				using (var reader = command.ExecuteReader( CommandBehavior.CloseConnection))
				{
					if(!reader.HasRows)
						return false;

					reader.Read(); // there must be only one record

					eTag= new TimedEntityTagHeaderValue((string) reader[ColumnNames.ETag])
					      	{
					      		LastModified = (DateTime) reader[ColumnNames.LastModified]
					      	};
					return true;
				}
			}
		}

		public void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag)
		{
			using (var connection = new SqlConnection(this._connectionString))
			using (var command = new SqlCommand())
			{
				connection.Open();
				command.Connection = connection;
				command.CommandText = this.GetStoredProcedureName(StoredProcedureNames.AddUpdateCache);
				command.CommandType = CommandType.StoredProcedure;
				command.Parameters.AddWithValue(ColumnNames.CacheKeyHash, key.Hash);
				command.Parameters.AddWithValue(ColumnNames.RoutePattern, key.RoutePattern);
				command.Parameters.AddWithValue(ColumnNames.ETag, eTag.Tag);
				command.Parameters.AddWithValue(ColumnNames.LastModified, eTag.LastModified);
				command.ExecuteNonQuery();
			}
		}

		public bool TryRemove(CacheKey key)
		{
			using (var connection = new SqlConnection(this._connectionString))
			using (var command = new SqlCommand())
			{
				connection.Open();
				command.Connection = connection;
				command.CommandText = this.GetStoredProcedureName(StoredProcedureNames.DeleteCacheById);
				command.CommandType = CommandType.StoredProcedure;
				command.Parameters.AddWithValue(ColumnNames.CacheKeyHash, key.Hash);
				return command.ExecuteNonQuery() > 0;			
			}
		}

		public int RemoveAllByRoutePattern(string routePattern)
		{
			using (var connection = new SqlConnection(this._connectionString))
			using (var command = new SqlCommand())
			{
				connection.Open();
				command.Connection = connection;
				command.CommandText = this.GetStoredProcedureName(StoredProcedureNames.DeleteCacheByRoutePattern);
				command.CommandType = CommandType.StoredProcedure;
				command.Parameters.AddWithValue(ColumnNames.RoutePattern, routePattern);
				return command.ExecuteNonQuery();
			}
		}

		public void Clear()
		{
			using (var connection = new SqlConnection(this._connectionString))
			using (var command = new SqlCommand())
			{
				connection.Open();
				command.Connection = connection;
				command.CommandText = this.GetStoredProcedureName(StoredProcedureNames.Clear);
				command.CommandType = CommandType.StoredProcedure;
				command.ExecuteNonQuery();
			}
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
	}
}
