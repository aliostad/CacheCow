using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace CacheCow.Client.Tests
{
    public class InMemoryVaryHeaderStoreTests
    {
        private const string TestUrl = "/api/Test?a=1";
        [Fact]
        public void Test_Insert_Get()
        {
            // arrange
            var store = new InMemoryVaryHeaderStore();
            IEnumerable<String> headers = null;
            var hdrs = new string[] {"a", "b"};

            // act
            store.AddOrUpdate(TestUrl, hdrs);
            var result = store.TryGetValue(TestUrl, out headers);

            // assert
            Assert.True(result);
            Assert.NotNull(headers);
            Assert.Equal(hdrs, headers);
        }

        [Fact]
        public void Test_Insert_remove()
        {
            // arrange
            var store = new InMemoryVaryHeaderStore();
            IEnumerable<String> headers = null;
            var hdrs = new string[] { "a", "b" };

            // act
            store.AddOrUpdate(TestUrl, hdrs);
            var tryRemove = store.TryRemove(TestUrl);
            var result = store.TryGetValue(TestUrl, out headers);

            // assert
            Assert.False(result);
            Assert.Null(headers);
            Assert.True(tryRemove);
            
        }

        [Fact]
        public void Test_Get_NonExisting()
        {
            // arrange
            var store = new InMemoryVaryHeaderStore();
            IEnumerable<String> headers = null;

            // act
            var result = store.TryGetValue(TestUrl, out headers);

            // assert
            Assert.False(result);
            Assert.Null(headers);

        }
    }
}
