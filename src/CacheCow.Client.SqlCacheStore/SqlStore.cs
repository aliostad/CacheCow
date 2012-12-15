using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using CacheCow.Common;
using Dapper;

namespace CacheCow.Client.SqlCacheStore
{
	public class SqlStore : ICacheStore, ICacheMetadataProvider
	{

		private const string ConnectionStringName = "CacheStore";
		private string _connectionString;
	    private IHttpMessageSerializerAsync _serializer = new MessageContentHttpMessageSerializer();

		/// <summary>
		/// Assumes connection is defined in a connection string called "CacheStore"
		/// </summary>
		public SqlStore()
		{
		    var connectionStringSettings = ConfigurationManager.ConnectionStrings[ConnectionStringName];
            if (connectionStringSettings == null || string.IsNullOrEmpty(connectionStringSettings.ConnectionString))
                throw new InvalidOperationException("Expecting a connection string called CacheStore");
            Init(connectionStringSettings.ConnectionString);
		}

	    /// <summary>
		/// 
		/// </summary>
		public SqlStore(string connectionString)
		{
            Init(connectionString);
		}

        private void Init(string connectionString)
        {
            _connectionString = connectionString;
            
        }

        public bool TryGetValue(CacheKey key, out HttpResponseMessage response)
        {
            response = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                IEnumerable<dynamic> result = connection.Query("GetCache",
                                                               param: new {CacheKeyHash = key.Hash},
                                                               commandType: CommandType.StoredProcedure);

                if(result.Any())
                {
                    response = _serializer.DeserializeToResponseAsync(new MemoryStream(
                                                               (byte[]) result.First().CacheBlob)).Result;
                    // TODO: change to async
                    return true;
                }

                return false;
            }
        }

	    public void AddOrUpdate(CacheKey key, HttpResponseMessage response)
		{
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var memoryStream = new MemoryStream();
                _serializer.SerializeAsync(TaskHelpers.FromResult(response), memoryStream)
                    .Wait(); // TODO: change to async

                connection.Execute("AddUpdateCache",
                                   param: new
                                              {
                                                  CacheKeyHash = key.Hash,
                                                  Domain = new Uri(key.ResourceUri).Host,
                                                  Size = (int) memoryStream.Length,
                                                  CacheBlob = memoryStream.ToArray()
                                              },
                                   commandType: CommandType.StoredProcedure);
            }
		}

		public bool TryRemove(CacheKey key)
		{
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                IEnumerable<dynamic> result = connection.Query("DeleteCacheById",
                                 param: new
                                            {
                                                CacheKeyHash = key.Hash
                                            },
                                 commandType: CommandType.StoredProcedure);

                if (!result.Any())
                    return false;

                return Convert.ToBoolean(result.First().Deleted);
            }
		}

		public void Clear()
		{
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                connection.Execute("TRUNCATE TABLE CACHE;");
            }
		}

		public IDictionary<string, long> GetDomainSizes()
		{
		    var dictionary = new Dictionary<string, long>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                IEnumerable<dynamic> result = connection.Query("GetDomainSizes",
                    commandType: CommandType.StoredProcedure);
                foreach (dynamic row in result)
                {
                    dictionary.Add(row.Domain, row.TotalSize);
                }
            }

		    return dictionary;
		}

	    public CacheItemMetadata GetEarliestAccessedItem(string domain)
		{
	        using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                IEnumerable<dynamic> result = connection.Query("GetDomainSizes",
                                                    param: new {Domain = domain},
                                                    commandType: CommandType.StoredProcedure);

                if (!result.Any())
                    return null;

                var first = result.First();
                return
                    new CacheItemMetadata()
                        {
                            Domain = first.Domain,
                            Key = first.CacheKeyHash,
                            LastAccessed = first.LastAccessed,
                            Size = first.Size
                        };

            }

	        
		}

		public CacheItemMetadata GetEarliestAccessedItem()
		{
		    return GetEarliestAccessedItem("");
		}
	}
}
