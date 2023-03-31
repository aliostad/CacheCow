namespace CacheCow.Client.NatsKeyValueCacheStore;

using System.Net.Http;
using System.Threading.Tasks;
using CacheCow.Common;
using NATS.Client;
using NATS.Client.KeyValue;

public class NatsKeyValueStore : ICacheStore
{
    private readonly string _bucketName;
    private readonly Options _options;
    private readonly ConnectionFactory _connectionFactory = new ConnectionFactory();
    private MessageContentHttpMessageSerializer _serializer = new MessageContentHttpMessageSerializer();



    public NatsKeyValueStore(string bucketName, Options options)
    {
        this._bucketName = bucketName;
        this._options = options;
    }

    public async Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
    {
        using (var kvc = new KeyValueContext(_bucketName, _options, _connectionFactory))
        {
            var ms = new MemoryStream();
            await _serializer.SerializeAsync(response, ms);
            var buffer = ms.ToArray();
            kvc.KeyValueStore.Put(new SantisedCacheKey(key).ToString(), buffer);
        }
    }

    public Task ClearAsync()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        // nothing
    }

    public async Task<HttpResponseMessage> GetValueAsync(CacheKey key)
    {
        using (var kvc = new KeyValueContext(_bucketName, _options, _connectionFactory))
        {
            var res = kvc.KeyValueStore.Get(new SantisedCacheKey(key).ToString());
            if (res == null)
                return (HttpResponseMessage) null;
            return await _serializer.DeserializeToResponseAsync(new MemoryStream(res.Value));
        }
    }

    public Task<bool> TryRemoveAsync(CacheKey key)
    {
        using (var kvc = new KeyValueContext(_bucketName, _options, _connectionFactory))
        {
            kvc.KeyValueStore.Delete(new SantisedCacheKey(key).ToString());
            return Task.FromResult(true);
        }
    }

    private class SantisedCacheKey
    {
        private readonly string _sanity;

        public SantisedCacheKey(CacheKey key)
        {
            _sanity = key.HashBase64.Replace('/', '_').Replace('+', '_').Replace('=', '_');
        }

        public override string ToString()
        {
            return _sanity;
        }
    }


    private class KeyValueContext : IDisposable
    {
        private readonly IConnection _connection;

        public KeyValueContext(string bucketName, Options options, ConnectionFactory factory)
        {
            _connection = factory.CreateConnection(options);
            var kvm = _connection.CreateKeyValueManagementContext();
            var kvc = KeyValueConfiguration.Builder()
                .WithName(bucketName)
                .Build();
            var keyValueStatus = kvm.Create(kvc);
            KeyValueStore = _connection.CreateKeyValueContext(bucketName);
        }

        public IKeyValue KeyValueStore { get; private set; }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
