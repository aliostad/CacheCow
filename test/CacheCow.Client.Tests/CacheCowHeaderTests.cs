using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using CacheCow.Client.Headers;
using Xunit;

namespace CacheCow.Client.Tests
{
	
	public class CacheCowHeaderTests
	{
		private string _version = Assembly.GetAssembly(typeof(CacheCowHeader))
				.GetName().Version.ToString();


        [Fact]
	    public void DoesNotThrow_IfHeadersNull()
        {
            HttpResponseHeaders headers = null;
            Assert.Null(headers.GetCacheCowHeader());
        }

		[Fact]
		public void ParseTest_Successful()
		{
			CacheCowHeader header;
			var result = CacheCowHeader.TryParse("1.0;was-stale=true;not-cacheable=false;retrieved-from-cache=true;", out header);
			Assert.Equal(true, result);
			Assert.Equal("1.0", header.Version);
			Assert.Equal(true, header.WasStale);
			Assert.Equal(true, header.RetrievedFromCache);
			Assert.Equal(false, header.NotCacheable);
			Assert.Equal(null, header.DidNotExist);
			Assert.Equal(null, header.CacheValidationApplied);
		}

		[Fact]
		public void ToStringTest_Successful()
		{
			var cacheCowHeader = new CacheCowHeader()
			                     	{
			                     		CacheValidationApplied = true,
			                     		DidNotExist = false
			                     	};

			var s = cacheCowHeader.ToString();
			Console.WriteLine(s);
			Assert.True(s.StartsWith(_version));
			Assert.True(s.IndexOf(CacheCowHeader.ExtensionNames.CacheValidationApplied + "=true") > 0);
			Assert.True(s.IndexOf(CacheCowHeader.ExtensionNames.DidNotExist + "=false") > 0);

		}
	}
}
