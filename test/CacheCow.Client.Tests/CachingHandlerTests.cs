using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Client;
using CacheCow.Client.Headers;
using CacheCow.Common;
using Moq;
using NUnit.Framework;

namespace CacheCow.Client.Tests
{
	[TestFixture]
	public class CachingHandlerTests
	{
		private const string DummyUrl = "http://myserver/api/dummy";
		private const string ETagValue = "\"abcdef\"";
		private HttpClient _client;
        private Mock<ICacheStore> _cacheStore;
		private DummyMessageHandler _messageHandler;
	    private CachingHandler _cachingHandler;

		[SetUp]
		public void Setup()
		{
			_cacheStore = new Mock<ICacheStore>();
			_messageHandler = new DummyMessageHandler();
            _cachingHandler = new CachingHandler(_cacheStore.Object)
		                             {
		                                 InnerHandler = _messageHandler
		                             };

            _client = new HttpClient(_cachingHandler);
		}

		[Test]
		public void Methods_Other_Than_PUT_GET_Ignored()
		{
			var request = new HttpRequestMessage(HttpMethod.Delete, DummyUrl);
			var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
			_messageHandler.Response = httpResponseMessage;
			var task = _client.SendAsync(request);
			var response = task.Result;

			Assert.AreEqual(response, httpResponseMessage);
			Assert.IsNull(response.Headers.CacheControl);
			Assert.IsNull(request.Headers.CacheControl);
		}

		[Test]
		public void NoStore_Ignored()
		{
			var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
			request.Headers.CacheControl = new CacheControlHeaderValue();
			request.Headers.CacheControl.NoStore = true;
			var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
			_messageHandler.Response = httpResponseMessage;
			var task = _client.SendAsync(request);
			var response = task.Result;

			Assert.AreEqual(response, httpResponseMessage);
			Assert.IsNull(response.Headers.CacheControl);

		}

        [Test]
        public void TestMemoryLeak()
        {
            var memorySize64 = Process.GetCurrentProcess().PrivateMemorySize64;
            for (int i = 0; i < 200; i++)
            {
                var store = new CachingHandler();
                //Thread.Sleep(1);
                store.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (Process.GetCurrentProcess().PrivateMemorySize64 - memorySize64 > 2 * 1024 * 1024)
                    Assert.Fail("Memory leak");
            }
        }

		[Test]
		public void Get_OK_But_Not_In_Cache_To_Insert_In_Cache()
		{
			// setup 
			var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
			var response = GetOkMessage();
			_messageHandler.Response = response;
			_cacheStore.Setup(x => x.GetValueAsync(It.IsAny<CacheKey>())).ReturnsAsync((HttpResponseMessage) null);
			_cacheStore.Setup(x => x.AddOrUpdateAsync(It.IsAny<CacheKey>(),
             It.Is<HttpResponseMessage>(y => y == response))).Returns(Task.FromResult(response));

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;
			var header = responseReturned.Headers.Single(x=>x.Key == CacheCowHeader.Name);
			CacheCowHeader cacheCowHeader = null;
			CacheCowHeader.TryParse(header.Value.First() , out cacheCowHeader);
			
            // verify
			Assert.IsNotNull(cacheCowHeader);
			Assert.AreEqual(true, cacheCowHeader.DidNotExist);


		}

		[Test]
		public void Get_Stale_And_In_Cache_To_Get_From_Cache()
		{
			// setup 
			var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
			var response = GetOkMessage();
			_messageHandler.Response = response;
			_cacheStore.Setup(x => x.GetValueAsync(It.IsAny<CacheKey>())).ReturnsAsync(response);
						

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;
			var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
			CacheCowHeader cacheCowHeader = null;
			CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

			// verify
			Assert.IsNotNull(cacheCowHeader);
			Assert.AreSame(response, responseReturned);
			Assert.AreEqual(true, cacheCowHeader.RetrievedFromCache);

		}



		[Test]
		public void Get_Stale_ApplyValidation_NotModified()
		{
			// setup 
			var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
			var responseFromCache = GetOkMessage();
		    var then = DateTimeOffset.UtcNow.AddMilliseconds(-1);
		    responseFromCache.Headers.Date = then;
            responseFromCache.Content.Headers.Expires = DateTimeOffset.Now.AddDays(-1);
            responseFromCache.Content.Headers.LastModified = DateTimeOffset.Now.AddDays(-2);
			var responseFromServer = GetOkMessage();
            responseFromServer.StatusCode = HttpStatusCode.NotModified;
		    
			_messageHandler.Response = responseFromServer;
            _cacheStore.Setup(x => x.GetValueAsync(It.IsAny<CacheKey>())).ReturnsAsync(responseFromCache);
            _cacheStore.Setup(x => x.AddOrUpdateAsync(It.IsAny<CacheKey>(), It.Is<HttpResponseMessage>(r => r == responseFromCache)))
                .Returns(Task.FromResult(false));

			

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;
			var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
			CacheCowHeader cacheCowHeader = null;
			CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

			// verify
			Assert.IsNotNull(cacheCowHeader);
			Assert.AreSame(responseFromCache, responseReturned);
            Assert.AreEqual(true, cacheCowHeader.WasStale);
            Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);
            Assert.AreNotEqual(then, responseFromCache.Headers.Date);

		}

		[Test]
		public void Get_Must_Revalidate_Etag_NotModified()
		{
			// setup 
			var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
			var responseFromCache = GetOkMessage(true);
			responseFromCache.Headers.ETag = new EntityTagHeaderValue(ETagValue);
			responseFromCache.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));
			var responseFromServer = new HttpResponseMessage(HttpStatusCode.NotModified);
			_messageHandler.Response = responseFromServer;
            _cacheStore.Setup(x => x.GetValueAsync(It.IsAny<CacheKey>())).ReturnsAsync(responseFromCache);
			_cacheStore.Setup(x => x.AddOrUpdateAsync(It.IsAny<CacheKey>(), It.IsAny<HttpResponseMessage>()))
                .Returns(Task.FromResult(true));

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;
			var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
			CacheCowHeader cacheCowHeader = null;
			CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

			// verify
			Assert.IsNotNull(cacheCowHeader);
			Assert.AreEqual(ETagValue, request.Headers.IfNoneMatch.First().Tag);
			Assert.AreSame(responseFromCache, responseReturned);
			Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);

		}

		[Test]
		public void Get_Must_Revalidate_Expires_NotModified()
		{
			// setup 
			var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
			var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
			lastModified = lastModified.AddMilliseconds(1000 - lastModified.Millisecond);
			var responseFromCache = GetOkMessage(true);
			responseFromCache.Content.Headers.LastModified = lastModified;
			responseFromCache.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));

			var responseFromServer = new HttpResponseMessage(HttpStatusCode.NotModified);
			_messageHandler.Response = responseFromServer;
            _cacheStore.Setup(x => x.GetValueAsync(It.IsAny<CacheKey>())).ReturnsAsync(responseFromCache);
			_cacheStore.Setup(x => x.AddOrUpdateAsync(It.IsAny<CacheKey>(), It.IsAny<HttpResponseMessage>()))
                .Returns(Task.FromResult(false));

			

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;
			var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
			CacheCowHeader cacheCowHeader = null;
			CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

			// verify
			Assert.IsNotNull(cacheCowHeader);
			Assert.AreEqual(lastModified.ToString(), request.Headers.IfModifiedSince.Value.ToString());
			Assert.AreSame(responseFromCache, responseReturned);
			Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);

		}

		[Test]
		public void Get_Must_Revalidate_Expires_Modified()
		{
			// setup 
			var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
			var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
			lastModified = lastModified.AddMilliseconds(1000 - lastModified.Millisecond);
			var responseFromCache = GetOkMessage(true);
			responseFromCache.Content.Headers.LastModified = lastModified;
			var responseFromServer = GetOkMessage();
			responseFromCache.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));

			_messageHandler.Response = responseFromServer;
            _cacheStore.Setup(x => x.GetValueAsync(It.IsAny<CacheKey>())).ReturnsAsync(responseFromCache);
			_cacheStore.Setup(x => x.AddOrUpdateAsync(It.IsAny<CacheKey>(),
				  It.Is<HttpResponseMessage>(r => r == responseFromServer))).Returns(Task.FromResult(true));


			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;
			var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
			CacheCowHeader cacheCowHeader = null;
			CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

			// verify
			Assert.IsNotNull(cacheCowHeader);
			Assert.AreSame(responseFromServer, responseReturned);
			Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);

		}

        [Test]
        public void Get_NoMustRevalidate_Expires_Modified()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
            var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
            lastModified = lastModified.AddMilliseconds(1000 - lastModified.Millisecond);
            var responseFromCache = GetOkMessage(false);
            responseFromCache.Content.Headers.LastModified = lastModified;
            var responseFromServer = GetOkMessage();
            responseFromCache.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));

            _messageHandler.Response = responseFromServer;
            _cacheStore.Setup(x => x.GetValueAsync(It.IsAny<CacheKey>())).ReturnsAsync(responseFromCache);
            _cacheStore.Setup(x => x.AddOrUpdateAsync(It.IsAny<CacheKey>(),
                  It.Is<HttpResponseMessage>(r => r == responseFromServer))).Returns(Task.FromResult(responseFromServer));

            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;
            var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
            CacheCowHeader cacheCowHeader = null;
            CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

            // verify
            Assert.IsNotNull(cacheCowHeader);
            Assert.AreSame(responseFromServer, responseReturned);
            Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);

        }

        [Test]
        public void Get_NoCache_Expires_ResultsInValidation()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
            request.Headers.CacheControl = new CacheControlHeaderValue(){NoCache = true};
            var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
            lastModified = lastModified.AddMilliseconds(1000 - lastModified.Millisecond);
            var responseFromCache = GetOkMessage(false);
            responseFromCache.Content.Headers.LastModified = lastModified;
            var responseFromServer = GetOkMessage();
            responseFromCache.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));

            _messageHandler.Response = responseFromServer;
            _cacheStore.Setup(x => x.GetValueAsync(It.IsAny<CacheKey>())).ReturnsAsync(responseFromCache);
            _cacheStore.Setup(x => x.AddOrUpdateAsync(It.IsAny<CacheKey>(),
                  It.Is<HttpResponseMessage>(r => r == responseFromServer))).Returns(Task.FromResult(responseFromServer));

            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;
            var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
            CacheCowHeader cacheCowHeader = null;
            CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

            // verify
            Assert.IsNotNull(cacheCowHeader);
            Assert.AreSame(responseFromServer, responseReturned);
            Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);

        }

        [Test]
        public void Get_NoMustRevalidate_NoMustRevalidateByDefault_Expires_GetFromCache()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
            var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
            lastModified = lastModified.AddMilliseconds(1000 - lastModified.Millisecond);
            var responseFromCache = GetOkMessage(false); // NOTE !!
            _cachingHandler.MustRevalidateByDefault = false; // NOTE!!
            responseFromCache.Content.Headers.LastModified = lastModified;
            var responseFromServer = GetOkMessage();
            responseFromCache.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));

            _messageHandler.Response = responseFromServer;
            _cacheStore.Setup(x => x.GetValueAsync(It.IsAny<CacheKey>())).ReturnsAsync(responseFromCache);


            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;
            var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
            CacheCowHeader cacheCowHeader = null;
            CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

            // verify
            Assert.IsNotNull(cacheCowHeader);
            Assert.AreSame(responseFromCache, responseReturned);
            Assert.AreEqual(true, cacheCowHeader.WasStale);

        }

		[Test]
		public void Get_NotModified_With_Stale_Client_Cache_Shall_Update_Date_Header()
		{
			// setup 
			var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
			
			var responseFromCache = GetOkMessage(false);
			responseFromCache.Headers.Date = DateTimeOffset.UtcNow.AddHours(-1);
			responseFromCache.Headers.CacheControl.MaxAge = TimeSpan.FromSeconds(10);

			var responseFromServer = new HttpResponseMessage(HttpStatusCode.NotModified) {Content = new ByteArrayContent(new byte[256])};

			_messageHandler.Response = responseFromServer;
            _cacheStore.Setup(x => x.GetValueAsync(It.IsAny<CacheKey>())).ReturnsAsync(responseFromCache);
			_cacheStore.Setup(x => x.AddOrUpdateAsync(It.IsAny<CacheKey>(), It.Is<HttpResponseMessage>(r => DateTimeOffset.UtcNow - r.Headers.Date.Value <= TimeSpan.FromSeconds(1))))
                .Returns(Task.FromResult(0));


			// run
            var responseReturned = _client.SendAsync(request).Result;
			var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
			CacheCowHeader cacheCowHeader;
			CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);


			// verify
			Assert.IsNotNull(cacheCowHeader);
			Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);
			Assert.AreEqual(true, cacheCowHeader.WasStale);
		}

		[Test]
		public void Put_Validate_Etag()
		{
			// setup 
			var request = new HttpRequestMessage(HttpMethod.Put, DummyUrl);
			var responseFromCache = GetOkMessage(true);
			responseFromCache.Headers.ETag = new EntityTagHeaderValue(ETagValue);
			var responseFromServer = new HttpResponseMessage(HttpStatusCode.NotModified);
			_messageHandler.Response = responseFromServer;
			_cacheStore.Setup(x => x.GetValueAsync(It.IsAny<CacheKey>())).ReturnsAsync(responseFromCache);

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;

			// verify
			Assert.AreEqual(ETagValue, request.Headers.IfMatch.First().Tag);
			Assert.AreSame(responseFromServer, responseReturned);

		}

		[Test]
		public void Put_Validate_Expires()
		{
			// setup 
			var request = new HttpRequestMessage(HttpMethod.Put, DummyUrl);
			var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
			lastModified = lastModified.AddMilliseconds(1000 - lastModified.Millisecond);
			var responseFromCache = GetOkMessage(true);
			responseFromCache.Content.Headers.LastModified = lastModified;
			var responseFromServer = GetOkMessage();
			_messageHandler.Response = responseFromServer;
            _cacheStore.Setup(x => x.GetValueAsync(It.IsAny<CacheKey>())).ReturnsAsync(responseFromCache);

			

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;

			// verify
			Assert.AreEqual(lastModified.ToString(), request.Headers.IfUnmodifiedSince.Value.ToString());
			Assert.AreSame(responseFromServer, responseReturned);

		}

      
        [Test]
	    public void DoesNotDisposeCacheStoreIfPassedToIt()
        {
            var mock = new Moq.Mock<ICacheStore>(MockBehavior.Strict);
            var handler = new CachingHandler(mock.Object);
            handler.Dispose();
            mock.Verify();
        }

        [Test]
        public void DoesNotDisposeVaryHeaderStoreIfPassedToIt()
        {
            var mockcs = new Moq.Mock<ICacheStore>();
            var mockvh = new Moq.Mock<IVaryHeaderStore>(MockBehavior.Strict);
            var handler = new CachingHandler(mockcs.Object, mockvh.Object);
            handler.Dispose();
            mockvh.Verify();
        }

	    private HttpResponseMessage GetOkMessage(bool mustRevalidate = false)
		{
			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Headers.CacheControl = new CacheControlHeaderValue()
			{
				Public = true,
				MaxAge = TimeSpan.FromSeconds(200),
				MustRevalidate = mustRevalidate
			};
			response.Headers.Date = DateTimeOffset.UtcNow;
			response.Content = new ByteArrayContent(new byte[256]);
			return response;
		}


	}

    public class FaultyCacheStore : ICacheStore
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetValueAsync(CacheKey key)
        {
            throw new NotImplementedException();
        }

        public Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryRemoveAsync(CacheKey key)
        {
            throw new NotImplementedException();
        }

        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }
    }

}
