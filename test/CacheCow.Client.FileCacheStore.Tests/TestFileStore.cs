using System;
using CacheCow.Client.Headers;
using Xunit;
using Xunit.Abstractions;

namespace CacheCow.Client.FileCacheStore.Tests
{
    /// <summary>
    /// Simple test of caching a request to disk
    /// </summary>
    public class TestFileStore
    {

        private readonly ITestOutputHelper _output;

        public TestFileStore(ITestOutputHelper output)
        {
            _output = output;
        }

        private void log(string s)
        {
            _output.WriteLine(s);
        }


        /// <summary>
        /// Test caching
        /// </summary>
        [Fact]
        public void TestDisk()
        {
            var fs = new FileStore("cache");
            fs.ClearAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            var client = fs.CreateClient();
            Assert.True(fs.IsEmpty());
            log("Querying...");
            var response = client.GetAsync(new Uri("https://www.example.org/")).ConfigureAwait(false).GetAwaiter().GetResult();
            Assert.False(fs.IsEmpty());
            var cachedResponse = client.GetAsync(new Uri("https://www.example.org/")).ConfigureAwait(false).GetAwaiter()
                .GetResult();


            Assert.True(response.Headers.GetCacheCowHeader().ToString().Contains("did-not-exist=true"));
            var cacheHeader = cachedResponse.Headers.GetCacheCowHeader().ToString();
            Assert.True(cacheHeader.Contains("did-not-exist=false;retrieved-from-cache=true"));

            fs.ClearAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            Assert.True(fs.IsEmpty());
        }
    }
}
