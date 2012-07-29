using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using CacheCow.Client;
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
				new CachingHandler()
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
		public void With_NoStore_Ignored()
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
		public void With_NoCache_Ignored()
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
		public void With_Get_Does_not_Exist_InCache()
		{

		}
	}
}
