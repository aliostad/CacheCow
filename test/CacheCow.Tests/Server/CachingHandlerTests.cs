using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Common;
using CacheCow.Common.Helpers;
using CacheCow.Server;
using NUnit.Framework;
using Rhino.Mocks;

namespace CacheCow.Tests.Server
{
	public static class CachingHandlerTests
	{
		private const string TestUrl = "http://myserver/api/stuff/";
		private static readonly string[] EtagValues = new[] { "abcdefgh", "12345678" };

		[TestCase("DELETE")]
		[TestCase("PUT")]
		[TestCase("POST")]
		[TestCase("PATCH")]
		public static void TestCacheInvalidation(string method)
		{
			// setup
			var mocks = new MockRepository();
			var request = new HttpRequestMessage(new HttpMethod(method), TestUrl);
			string routePattern = "http://myserver/api/stuffs/*";
			var entityTagStore = mocks.StrictMock<IEntityTagStore>();
			var linkedUrls = new []{"url1", "url2"};
			var cachingHandler = new CachingHandler(entityTagStore)
									{
										LinkedRoutePatternProvider = (url, mthd) => linkedUrls
									};
			var entityTagKey = new CacheKey(TestUrl, new string[0], routePattern);
			var response = new HttpResponseMessage();
			var invalidateCache = cachingHandler.InvalidateCache(entityTagKey, request, response);
			entityTagStore.Expect(x => x.RemoveAllByRoutePattern(routePattern)).Return(1);
			entityTagStore.Expect(x => x.RemoveAllByRoutePattern(linkedUrls[0])).Return(0);
			entityTagStore.Expect(x => x.RemoveAllByRoutePattern(linkedUrls[1])).Return(0);
			mocks.ReplayAll();

			// run
			invalidateCache();

			// verify
			mocks.VerifyAll();


		}

		[TestCase("PUT")]
		[TestCase("POST")]
        [TestCase("PATCH")]
		public static void TestCacheInvalidationForPost(string method)
		{
			// setup
			var locationUrl = new Uri("http://api/SomeLocationUrl");
			var mocks = new MockRepository();
			var request = new HttpRequestMessage(new HttpMethod(method), TestUrl);
			string routePattern = "http://myserver/api/stuffs/*";
			var entityTagStore = mocks.StrictMock<IEntityTagStore>();
			var linkedUrls = new[] { "url1", "url2" };
			var cachingHandler = new CachingHandler(entityTagStore)
			{
				LinkedRoutePatternProvider = (url, mthd) => linkedUrls
			};
			var entityTagKey = new CacheKey(TestUrl, new string[0], routePattern);
			var response = new HttpResponseMessage();
			response.Headers.Location = locationUrl;
			var invalidateCacheForPost = cachingHandler.PostInvalidationRule(entityTagKey, request, response);
			if(method == "POST")
			{
				entityTagStore.Expect(x => x.RemoveAllByRoutePattern(locationUrl.ToString())).Return(1);				
			}
			mocks.ReplayAll();

			// run
			invalidateCacheForPost();

			// verify
			mocks.VerifyAll();

			
		}

		[TestCase("GET", true, true, true, false, new[] { "Accept", "Accept-Language" })]
		[TestCase("GET", false, true, true, false, new[] { "Accept", "Accept-Language" })]
		[TestCase("PUT", false, true, false, false, new string[0])]
		[TestCase("PUT", false, false, true, true, new[] { "Accept" })]
		public static void AddCaching(string method,
			bool existsInStore,
			bool addVaryHeader,
			bool addLastModifiedHeader,
			bool alreadyHasLastModified,
			string[] varyByHeader)
		{
			// setup 
			var mocks = new MockRepository();
			var request = new HttpRequestMessage(new HttpMethod(method), TestUrl);
			request.Headers.Add(HttpHeaderNames.Accept, "text/xml");
			request.Headers.Add(HttpHeaderNames.AcceptLanguage, "en-GB");
			var entityTagStore = mocks.StrictMock<IEntityTagStore>();
			var entityTagHeaderValue = new TimedEntityTagHeaderValue("\"12345678\"");
			var cachingHandler = new CachingHandler(entityTagStore, varyByHeader)
			{
				AddLastModifiedHeader = addLastModifiedHeader,
				AddVaryHeader = addVaryHeader,
				ETagValueGenerator = (x,y) => entityTagHeaderValue
			};

			
			var entityTagKey = new CacheKey(TestUrl, new[] {"text/xml", "en-GB"}, TestUrl + "/*");

			entityTagStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Matches(etg => etg.ResourceUri == TestUrl),
				out Arg<TimedEntityTagHeaderValue>.Out(entityTagHeaderValue).Dummy)).Return(existsInStore);

			if (!existsInStore)
			{
				entityTagStore.Expect(
					x => x.AddOrUpdate(Arg<CacheKey>.Matches(etk => etk == entityTagKey),
						Arg<TimedEntityTagHeaderValue>.Matches(ethv => ethv.Tag == entityTagHeaderValue.Tag)));
			}

			var response = new HttpResponseMessage();
			response.Content = new ByteArrayContent(new byte[0]);
			if (alreadyHasLastModified)
				response.Content.Headers.Add(HttpHeaderNames.LastModified, DateTimeOffset.Now.ToString("r"));

			var cachingContinuation = cachingHandler.AddCaching(entityTagKey, request, response, request.Headers);
			mocks.ReplayAll();

			// run
			cachingContinuation();

			// verify

			// test kast modified only if it is GET and PUT
			if (addLastModifiedHeader && method.IsIn("PUT", "GET"))
			{
				Assert.That(response.Content.Headers.Any(x => x.Key == HttpHeaderNames.LastModified),
					"LastModified does not exist");
			}
			if (!addLastModifiedHeader && !alreadyHasLastModified)
			{
				Assert.That(!response.Content.Headers.Any(x => x.Key == HttpHeaderNames.LastModified),
					"LastModified exists");
			}
			mocks.VerifyAll();

		}

	}
}
