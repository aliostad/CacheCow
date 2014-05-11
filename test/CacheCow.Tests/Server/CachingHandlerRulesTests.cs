using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using CacheCow.Common;
using CacheCow.Server;
using NUnit.Framework;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using Rhino.Mocks.Expectations;
using Rhino.Mocks.Impl;

namespace CacheCow.Tests.Server
{
	public static class CachingHandlerRulesTests
	{
		private const string TestUrl = "http://myserver/api/stuff/";
		private static readonly string[] EtagValues = new []{"abcdefgh", "12345678"};

		[TestCase(HttpHeaderNames.IfNoneMatch, new[] { "\"abcdefgh\"", "\"12345678\"" }, false, true, HttpStatusCode.Unused)]
		[TestCase(HttpHeaderNames.IfNoneMatch, new[] { "\"abcdefgh\"", "\"12345678\"" }, true, false, HttpStatusCode.NotModified)]
		[TestCase(HttpHeaderNames.IfNoneMatch, new string[0], false, true, HttpStatusCode.Unused)]
		public static void GetMatchNonMatchTest(
			string headerName, 
			string[] values, 
			bool existsInStore,
			bool expectReturnNull,
			HttpStatusCode expectedStatus = HttpStatusCode.Unused)
		{
			// setup 
			var mocks = new MockRepository();
			var entityTagStore = mocks.StrictMock<IEntityTagStore>();
			var entityTagHandler = new CachingHandler(new HttpConfiguration(), entityTagStore);
			var request = new HttpRequestMessage(HttpMethod.Get, TestUrl);
			request.Headers.Add(headerName, values);
			TimedEntityTagHeaderValue entityTagHeaderValue = new TimedEntityTagHeaderValue("\"12345678\"");

			if(values.Length>0) // if 
				entityTagStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Matches(etg => etg.ResourceUri == entityTagHandler.UriTrimmer(new Uri(TestUrl))),
					out Arg<TimedEntityTagHeaderValue>.Out(entityTagHeaderValue).Dummy)).Return(existsInStore);

			mocks.ReplayAll();


			// run 
			var matchNoneMatch = entityTagHandler.GetIfMatchNoneMatch();
			// verify 
			Task<HttpResponseMessage> resultTask = matchNoneMatch(request);
			Assert.That(expectReturnNull ^ resultTask != null, "result was not as expected");
			if(resultTask!=null && expectedStatus != HttpStatusCode.Unused)
			{
				Assert.AreEqual(expectedStatus, resultTask.Result.StatusCode, "Status code");				
			}
			mocks.VerifyAll();
		}

		[TestCase]
		public static void TestGetReturnsNullIfVerbNotGet()
		{
			// setup
			var request = new HttpRequestMessage(HttpMethod.Put, TestUrl);
			var entityTagHandler = new CachingHandler(new HttpConfiguration());
			var getRule = entityTagHandler.GetIfMatchNoneMatch();
			
			// run
			var task = getRule(request);

			// verify
			Assert.IsNull(task);
		}

		[TestCase]
		public static void TestGetReturnsBadRequestIfBothIfMatchAndIfNoneMatchExist()
		{
			// setup
			var request = new HttpRequestMessage(HttpMethod.Get, TestUrl);
			request.Headers.Add(HttpHeaderNames.IfMatch, "\"123\"");
			request.Headers.Add(HttpHeaderNames.IfNoneMatch, "\"123\"");
            var entityTagHandler = new CachingHandler(new HttpConfiguration());
			var getRule = entityTagHandler.GetIfMatchNoneMatch();

			// run
			var task = getRule(request);
			var httpResponseMessage = task.Result;

			// verify
			Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseMessage.StatusCode);
		}

		[TestCase(HttpHeaderNames.IfModifiedSince, true, true, HttpStatusCode.Unused)]
		[TestCase(HttpHeaderNames.IfModifiedSince, false, false, HttpStatusCode.NotModified)]
		public static void GetModifiedNotModifiedTest(
				string headerName,
				bool resourceHasChanged,
				bool expectReturnNull,
				HttpStatusCode expectedStatus = HttpStatusCode.Unused)
		{
			// setup 
			var mocks = new MockRepository();
			var entityTagStore = mocks.StrictMock<IEntityTagStore>();
			var entityTagHandler = new CachingHandler(new HttpConfiguration(), entityTagStore);
			var request = new HttpRequestMessage(HttpMethod.Get, TestUrl);
			DateTimeOffset lastChanged = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(7));
			DateTimeOffset lastModifiedInQuestion = resourceHasChanged
			                                        	? lastChanged.Subtract(TimeSpan.FromDays(1))
			                                        	: lastChanged.Add(TimeSpan.FromDays(1));

			request.Headers.Add(headerName, lastModifiedInQuestion.ToString("r"));
			var entityTagHeaderValue = new TimedEntityTagHeaderValue("\"12345678\"")
				{LastModified = lastChanged};

			entityTagStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Matches(etg => etg.ResourceUri == entityTagHandler.UriTrimmer(new Uri(TestUrl))),
				out Arg<TimedEntityTagHeaderValue>.Out(entityTagHeaderValue).Dummy)).Return(true);

			mocks.ReplayAll();


			// run 
			var modifiedUnmodifiedSince = entityTagHandler.GetIfModifiedUnmodifiedSince();
			var task = modifiedUnmodifiedSince(request);
			HttpResponseMessage response = task == null ? null : task.Result;

			// verify 
			Assert.That(expectReturnNull ^ task != null, "result was not as expected");
			if (task != null && expectedStatus != HttpStatusCode.Unused)
			{
				Assert.AreEqual(expectedStatus, response.StatusCode, "Status code");
			}
			mocks.VerifyAll();

		}

		[TestCase]
		public static void TestGetModifiedUnmodifiedReturnsNullIfVerbNotGet()
		{
			// setup
			var request = new HttpRequestMessage(HttpMethod.Put, TestUrl);
            var entityTagHandler = new CachingHandler(new HttpConfiguration());
			var getRule = entityTagHandler.GetIfModifiedUnmodifiedSince();
			// run
			var task = getRule(request);

			// verify
			Assert.IsNull(task);
		}

		[TestCase]
		public static void TestGetModifiedUnmodifiedReturnsNullIfNoneDefined()
		{
			// setup
			var request = new HttpRequestMessage(HttpMethod.Get, TestUrl);
            var entityTagHandler = new CachingHandler(new HttpConfiguration());
			var getRule = entityTagHandler.GetIfModifiedUnmodifiedSince();
			// run
			var task = getRule(request);

			// verify
			Assert.IsNull(task);
		}

		[TestCase]
		public static void TestGetReturnsBadRequestIfBothIfModifiedAndIfUnmodifiedExist()
		{
			// setup
			var request = new HttpRequestMessage(HttpMethod.Get, TestUrl);
			request.Headers.Add(HttpHeaderNames.IfModifiedSince, DateTimeOffset.Now.ToString("r"));
			request.Headers.Add(HttpHeaderNames.IfUnmodifiedSince, DateTimeOffset.Now.ToString("r"));
            var entityTagHandler = new CachingHandler(new HttpConfiguration());
			var getRule = entityTagHandler.GetIfModifiedUnmodifiedSince();

			// run
			var task = getRule(request);
			var httpResponseMessage = task.Result;

			// verify
			Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseMessage.StatusCode);
		}

		[TestCase(false, true, HttpStatusCode.NotModified)]
		[TestCase(true, false, HttpStatusCode.PreconditionFailed)]
		public static void PutIfUnmodifiedTest(
				bool resourceHasChanged,
				bool expectReturnNull,
				HttpStatusCode expectedStatus = HttpStatusCode.Unused)
		{
			// setup 
			var mocks = new MockRepository();
			var entityTagStore = mocks.StrictMock<IEntityTagStore>();
			var entityTagHandler = new CachingHandler(new HttpConfiguration(), entityTagStore);
			var request = new HttpRequestMessage(HttpMethod.Put, TestUrl);
			DateTimeOffset lastChanged = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(7));
			DateTimeOffset lastModifiedInQuestion = resourceHasChanged
														? lastChanged.Subtract(TimeSpan.FromDays(1))
														: lastChanged.Add(TimeSpan.FromDays(1));

			request.Headers.Add(HttpHeaderNames.IfUnmodifiedSince, lastModifiedInQuestion.ToString("r"));
			TimedEntityTagHeaderValue entityTagHeaderValue = new TimedEntityTagHeaderValue("\"12345678\"") { LastModified = lastChanged };

			entityTagStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Matches(etg => etg.ResourceUri == entityTagHandler.UriTrimmer(new Uri(TestUrl))),
				out Arg<TimedEntityTagHeaderValue>.Out(entityTagHeaderValue).Dummy)).Return(true);

			mocks.ReplayAll();


			// run 
			var modifiedUnmodifiedSince = entityTagHandler.PutIfUnmodifiedSince();
			var task = modifiedUnmodifiedSince(request);
			HttpResponseMessage response = task == null ? null : task.Result;

			// verify 
			Assert.That(expectReturnNull ^ task != null, "result was not as expected");
			if (task != null && expectedStatus != HttpStatusCode.Unused)
			{
				Assert.AreEqual(expectedStatus, response.StatusCode, "Status code");
			}
			mocks.VerifyAll();

		}

		[TestCase(new[] { "\"abcdefgh\"", "\"12345678\"" }, false, false, HttpStatusCode.PreconditionFailed)]
		[TestCase(new[] { "\"abcdefgh\"", "\"12345678\"" }, true, true, HttpStatusCode.Unused)]
		public static void PutIfMatchTest(
			string[] values,
			bool existsInStore,
			bool expectReturnNull,
			HttpStatusCode expectedStatus = HttpStatusCode.Unused)
		{
			// setup 
			var mocks = new MockRepository();
			var entityTagStore = mocks.StrictMock<IEntityTagStore>();
			var entityTagHandler = new CachingHandler(new HttpConfiguration(), entityTagStore);
			var request = new HttpRequestMessage(HttpMethod.Put, TestUrl);
			request.Headers.Add(HttpHeaderNames.IfMatch, values);
			TimedEntityTagHeaderValue entityTagHeaderValue = new TimedEntityTagHeaderValue("\"12345678\"");

			if (values.Length > 0) // if 
				entityTagStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Matches(etg => etg.ResourceUri == entityTagHandler.UriTrimmer(new Uri(TestUrl))),
					out Arg<TimedEntityTagHeaderValue>.Out(entityTagHeaderValue).Dummy)).Return(existsInStore);

			mocks.ReplayAll();


			// run 
			var matchNoneMatch = entityTagHandler.PutIfMatch();
			// verify 
			Task<HttpResponseMessage> resultTask = matchNoneMatch(request);
			Assert.That(expectReturnNull ^ resultTask != null, "result was not as expected");
			if (resultTask != null && expectedStatus != HttpStatusCode.Unused)
			{
				Assert.AreEqual(expectedStatus, resultTask.Result.StatusCode, "Status code");
			}
			mocks.VerifyAll();
		}
		[TestCase]
		public static void TestPutIfUnmodifiedReturnsNullIfVerbNotPut()
		{
			// setup
			var request = new HttpRequestMessage(HttpMethod.Get, TestUrl);
            var entityTagHandler = new CachingHandler(new HttpConfiguration());
			var getRule = entityTagHandler.PutIfUnmodifiedSince();
			// run
			var task = getRule(request);

			// verify
			Assert.IsNull(task);
		}

		[TestCase]
		public static void TestPutIfMatchReturnsNullIfVerbNotPut()
		{
			// setup
			var request = new HttpRequestMessage(HttpMethod.Get, TestUrl);
            var entityTagHandler = new CachingHandler(new HttpConfiguration());
			var getRule = entityTagHandler.PutIfMatch();

			// run
			var task = getRule(request);

			// verify
			Assert.IsNull(task);
		}
	
	}
}
