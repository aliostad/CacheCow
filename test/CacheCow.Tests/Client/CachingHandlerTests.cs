using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using CacheCow.Client;
using CacheCow.Client.Headers;
using CacheCow.Common;
using CacheCow.Tests.Helper;
using NUnit.Framework;
using Rhino.Mocks;

namespace CacheCow.Tests.Client
{
	[TestFixture]
	public class CachingHandlerTests
	{
		private const string DummyUrl = "http://myserver/api/dummy";
		private const string ETagValue = "\"abcdef\"";
		private HttpClient _client;
		private ICacheStore _cacheStore;
		private MockRepository _mockRepository;
		private DummyMessageHandler _messageHandler;

		[SetUp]
		public void Setup()
		{
			_mockRepository = new MockRepository();
			_cacheStore = _mockRepository.StrictMock<ICacheStore>();
			_messageHandler = new DummyMessageHandler();
			_client = new HttpClient(
				new CachingHandler(_cacheStore)
					{
						InnerHandler = _messageHandler
					});
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
		public void NoCache_Ignored()
		{
			var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
			request.Headers.CacheControl = new CacheControlHeaderValue();
			request.Headers.CacheControl.NoCache = true;
			var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
			_messageHandler.Response = httpResponseMessage;
			var task = _client.SendAsync(request);
			var response = task.Result;

			Assert.AreEqual(response, httpResponseMessage);
			Assert.IsNull(response.Headers.CacheControl);

		}

		[Test]
		public void Get_OK_But_Not_In_Cache_To_Insert_In_Cache()
		{
			// setup 
			var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
			var response = GetOkMessage();
			_messageHandler.Response = response;
			_cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
			      out Arg<HttpResponseMessage>.Out(null).Dummy)).Return(false);
			_cacheStore.Expect(x => x.AddOrUpdate(Arg<CacheKey>.Is.Anything,
				  Arg<HttpResponseMessage>.Is.Same(response)));

			_mockRepository.ReplayAll();

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;
			var header = responseReturned.Headers.Single(x=>x.Key == CacheCowHeader.Name);
			CacheCowHeader cacheCowHeader = null;
			CacheCowHeader.TryParse(header.Value.First() , out cacheCowHeader);
			// verify
			_mockRepository.VerifyAll();

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
			_cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
				  out Arg<HttpResponseMessage>.Out(response).Dummy)).Return(true);
						
			_mockRepository.ReplayAll();

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;
			var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
			CacheCowHeader cacheCowHeader = null;
			CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);
			// verify
			_mockRepository.VerifyAll();
			Assert.IsNotNull(cacheCowHeader);
			Assert.AreSame(response, responseReturned);
			Assert.AreEqual(true, cacheCowHeader.RetrievedFromCache);

		}



		[Test]
		public void Get_Stale()
		{
			// setup 
			var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
			var responseFromCache = GetOkMessage();
			responseFromCache.Content.Headers.Expires = DateTimeOffset.Now.AddDays(-1);
			var responseFromServer = GetOkMessage();
			_messageHandler.Response = responseFromServer;
			_cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
				  out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy)).Return(true);
			_cacheStore.Expect(x => x.TryRemove(Arg<CacheKey>.Is.Anything)).Return(true);
			_cacheStore.Expect(x => x.AddOrUpdate(Arg<CacheKey>.Is.Anything,
				  Arg<HttpResponseMessage>.Is.Same(responseFromServer)));

			_mockRepository.ReplayAll();

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;
			var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
			CacheCowHeader cacheCowHeader = null;
			CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

			// verify
			_mockRepository.VerifyAll();
			Assert.IsNotNull(cacheCowHeader);
			Assert.AreSame(responseFromServer, responseReturned);
			Assert.AreEqual(true, cacheCowHeader.WasStale);

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
			_cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
				  out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy)).Return(true);
			
			_mockRepository.ReplayAll();

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;
			var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
			CacheCowHeader cacheCowHeader = null;
			CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

			// verify
			_mockRepository.VerifyAll();
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
			_cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
				  out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy)).Return(true);

			_mockRepository.ReplayAll();

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;
			var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
			CacheCowHeader cacheCowHeader = null;
			CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

			// verify
			_mockRepository.VerifyAll();
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
			_cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
				  out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy)).Return(true);
			_cacheStore.Expect(x => x.AddOrUpdate(Arg<CacheKey>.Is.Anything,
				  Arg<HttpResponseMessage>.Is.Same(responseFromServer)));

			_mockRepository.ReplayAll();

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;
			var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
			CacheCowHeader cacheCowHeader = null;
			CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

			// verify
			_mockRepository.VerifyAll();
			Assert.IsNotNull(cacheCowHeader);
			Assert.AreSame(responseFromServer, responseReturned);
			Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);

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
			_cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
				  out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy)).Return(true);

			_mockRepository.ReplayAll();

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;

			// verify
			_mockRepository.VerifyAll();
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
			_cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
				  out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy)).Return(true);

			_mockRepository.ReplayAll();

			// run
			var task = _client.SendAsync(request);
			var responseReturned = task.Result;

			// verify
			_mockRepository.VerifyAll();
			Assert.AreEqual(lastModified.ToString(), request.Headers.IfUnmodifiedSince.Value.ToString());
			Assert.AreSame(responseFromServer, responseReturned);

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
}
