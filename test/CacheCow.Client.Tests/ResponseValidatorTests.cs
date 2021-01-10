using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using CacheCow.Client;
using Xunit;

namespace CacheCow.Client.Tests
{

	public class ResponseValidatorTests
	{
        [Theory]
		[InlineData(HttpStatusCode.NotFound)]
		[InlineData(HttpStatusCode.NotModified)]
		[InlineData(HttpStatusCode.InternalServerError)]
		public void Test_Not_Cacheable_StatusCode(HttpStatusCode code)
		{
			var cachingHandler = new CachingHandler();
			var response = new HttpResponseMessage(code);
			Assert.Equal(cachingHandler.ResponseValidator(response), ResponseValidationResult.NotCacheable);
		}

		[Fact]
		public void Test_Not_Cacheable_No_CacheControl()
		{
			var cachingHandler = new CachingHandler();
			var response = new HttpResponseMessage(HttpStatusCode.OK);
			Assert.Equal(cachingHandler.ResponseValidator(response), ResponseValidationResult.NotCacheable);
		}

		[Fact]
		public void Test_Not_Cacheable_No_Content()
		{
			var cachingHandler = new CachingHandler();
			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Headers.CacheControl = new CacheControlHeaderValue(){Public = true};
			Assert.Equal(cachingHandler.ResponseValidator(response), ResponseValidationResult.NotCacheable);
		}

		[Fact]
		public void Test_Not_Cacheable_No_Expiration()
		{
			var cachingHandler = new CachingHandler();
			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Headers.CacheControl = new CacheControlHeaderValue() { Public = true };
			response.Content = new ByteArrayContent(new byte[256]);
			Assert.Equal(cachingHandler.ResponseValidator(response), ResponseValidationResult.NotCacheable);
		}

        [Fact]
        public void Test_NoCache_IsCacheable_And_NotStale_But_MustRevalidate()
        {
            var cachingHandler = new CachingHandler();
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue() { Public = true, NoCache = true};
            response.Content = new ByteArrayContent(new byte[256]);
            response.Content.Headers.Expires = DateTimeOffset.Now.AddHours(1); // resource is not stale
            Assert.Equal(cachingHandler.ResponseValidator(response), ResponseValidationResult.MustRevalidate);
        }


		[Fact]
		public void Test_Stale_By_Expires()
		{
			var cachingHandler = new CachingHandler();
            cachingHandler.MustRevalidateByDefault = false;

			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Headers.CacheControl = new CacheControlHeaderValue() { Public = true };
			response.Content = new ByteArrayContent(new byte[256]);
			response.Content.Headers.Expires = DateTimeOffset.UtcNow.AddDays(-1);
			Assert.Equal(cachingHandler.ResponseValidator(response), ResponseValidationResult.Stale);
		}

		[Fact]
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
			Assert.Equal(cachingHandler.ResponseValidator(response), ResponseValidationResult.MustRevalidate);
		}

        [Fact]
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
            Assert.Equal(cachingHandler.ResponseValidator(response), ResponseValidationResult.Stale);
        }

		[Fact]
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
			Assert.Equal(cachingHandler.ResponseValidator(response), ResponseValidationResult.Stale);
		}

		[Fact]
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
			Assert.Equal(cachingHandler.ResponseValidator(response), ResponseValidationResult.MustRevalidate);
		}

		[Fact]
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
			Assert.Equal(cachingHandler.ResponseValidator(response), ResponseValidationResult.OK);
		}

        // issue #257
        [Fact]
        public void Test_Age()
        {
            var cachingHandler = new CachingHandler()
            {
                MustRevalidateByDefault = false
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(200),
                MustRevalidate = false
            };
            response.Headers.Age = TimeSpan.FromSeconds(300); // more than MaxAge

            response.Headers.Date = DateTimeOffset.UtcNow;
            response.Content = new ByteArrayContent(new byte[256]);
            Assert.Equal(ResponseValidationResult.Stale, cachingHandler.ResponseValidator(response));
        }

	}
}
