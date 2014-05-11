using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using CacheCow.Client;
using NUnit.Framework;

namespace CacheCow.Client.Tests
{
	[TestFixture]
	public class ResponseValidatorTests
	{
		[TestCase(HttpStatusCode.NotFound)]
		[TestCase(HttpStatusCode.NotModified)]
		[TestCase(HttpStatusCode.InternalServerError)]
		public void Test_Not_Cacheable_StatusCode(HttpStatusCode code)
		{
			var cachingHandler = new CachingHandler();
			var response = new HttpResponseMessage(code);
			Assert.AreEqual(cachingHandler.ResponseValidator(response), ResponseValidationResult.NotCacheable);
		}

		[Test]
		public void Test_Not_Cacheable_No_CacheControl()
		{
			var cachingHandler = new CachingHandler();
			var response = new HttpResponseMessage(HttpStatusCode.OK);
			Assert.AreEqual(cachingHandler.ResponseValidator(response), ResponseValidationResult.NotCacheable);
		}

		[Test]
		public void Test_Not_Cacheable_No_Content()
		{
			var cachingHandler = new CachingHandler();
			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Headers.CacheControl = new CacheControlHeaderValue(){Public = true};
			Assert.AreEqual(cachingHandler.ResponseValidator(response), ResponseValidationResult.NotCacheable);
		}

		[Test]
		public void Test_Not_Cacheable_No_Expiration()
		{
			var cachingHandler = new CachingHandler();
			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Headers.CacheControl = new CacheControlHeaderValue() { Public = true };
			response.Content = new ByteArrayContent(new byte[256]);
			Assert.AreEqual(cachingHandler.ResponseValidator(response), ResponseValidationResult.NotCacheable);
		}

        [Test]
        public void Test_NoCache_IsCacheable_And_NotStale_But_MustRevalidate()
        {
            var cachingHandler = new CachingHandler();
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue() { Public = true, NoCache = true};
            response.Content = new ByteArrayContent(new byte[256]);
            response.Content.Headers.Expires = DateTimeOffset.Now.AddHours(1); // resource is not stale
            Assert.AreEqual(cachingHandler.ResponseValidator(response), ResponseValidationResult.MustRevalidate);
        }


		[Test]
		public void Test_Stale_By_Expires()
		{
			var cachingHandler = new CachingHandler();
            cachingHandler.MustRevalidateByDefault = false;

			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Headers.CacheControl = new CacheControlHeaderValue() { Public = true };
			response.Content = new ByteArrayContent(new byte[256]);
			response.Content.Headers.Expires = DateTimeOffset.UtcNow.AddDays(-1);
			Assert.AreEqual(cachingHandler.ResponseValidator(response), ResponseValidationResult.Stale);
		}

		[Test]
		public void Test_Stale_By_Age()
		{
			var cachingHandler = new CachingHandler();
            var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Headers.CacheControl = new CacheControlHeaderValue()
			                                	{
			                                		Public = true,
													MaxAge = TimeSpan.FromSeconds(200)
			                                	};
			response.Headers.Date = DateTimeOffset.UtcNow.AddDays(-1);
			response.Content = new ByteArrayContent(new byte[256]);
			Assert.AreEqual(cachingHandler.ResponseValidator(response), ResponseValidationResult.MustRevalidate);
		}

        [Test]
        public void Test_Stale_By_Age_MustRevalidateByDefaultOFF()
        {
            var cachingHandler = new CachingHandler();
            cachingHandler.MustRevalidateByDefault = false;

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(200)
            };
            response.Headers.Date = DateTimeOffset.UtcNow.AddDays(-1);
            response.Content = new ByteArrayContent(new byte[256]);
            Assert.AreEqual(cachingHandler.ResponseValidator(response), ResponseValidationResult.Stale);
        }

		[Test]
		public void Test_Stale_By_SharedAge()
		{
			var cachingHandler = new CachingHandler();
            cachingHandler.MustRevalidateByDefault = false;

			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Headers.CacheControl = new CacheControlHeaderValue()
			{
				Public = true,
				SharedMaxAge = TimeSpan.FromSeconds(200)
			};
			response.Headers.Date = DateTimeOffset.UtcNow.AddDays(-1);
			response.Content = new ByteArrayContent(new byte[256]);
			Assert.AreEqual(cachingHandler.ResponseValidator(response), ResponseValidationResult.Stale);
		}

		[Test]
		public void Test_Must_Revalidate()
		{
			var cachingHandler = new CachingHandler();
			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Headers.CacheControl = new CacheControlHeaderValue()
			{
				Public = true,
				MaxAge = TimeSpan.FromSeconds(200),
				MustRevalidate = true
			};
			response.Headers.Date = DateTimeOffset.UtcNow;
			response.Content = new ByteArrayContent(new byte[256]);
			response.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));
			Assert.AreEqual(cachingHandler.ResponseValidator(response), ResponseValidationResult.MustRevalidate);
		}

		[Test]
		public void Test_OK()
		{
			var cachingHandler = new CachingHandler();
			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Headers.CacheControl = new CacheControlHeaderValue()
			{
				Public = true,
				MaxAge = TimeSpan.FromSeconds(200),
				MustRevalidate = false
			};
			response.Headers.Date = DateTimeOffset.UtcNow;
			response.Content = new ByteArrayContent(new byte[256]);
			Assert.AreEqual(cachingHandler.ResponseValidator(response), ResponseValidationResult.OK);
		}

	}
}
