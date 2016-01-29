using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using CacheCow.Client.Headers;
using NUnit.Framework;

namespace CacheCow.Client.Tests
{
	[TestFixture]
	public class CacheCowHeaderTests
	{
		private string _version = Assembly.GetAssembly(typeof(CacheCowHeader))
				.GetName().Version.ToString();


        [Test]
	    public void DoesNotThrow_IfHeadersNull()
        {
            HttpResponseHeaders headers = null;
            Assert.IsNull(headers.GetCacheCowHeader());
        }

		[Test]
		public void ParseTest_Successful()
		{
			CacheCowHeader header;
			var result = CacheCowHeader.TryParse("1.0;was-stale=true;not-cacheable=false;retrieved-from-cache=true;", out header);
			Assert.AreEqual(true, result);
			Assert.AreEqual("1.0", header.Version);
			Assert.AreEqual(true, header.WasStale);
			Assert.AreEqual(true, header.RetrievedFromCache);
			Assert.AreEqual(false, header.NotCacheable);
			Assert.AreEqual(null, header.DidNotExist);
			Assert.AreEqual(null, header.CacheValidationApplied);
		}

		[Test]
		public void ToStringTest_Successful()
		{
			var cacheCowHeader = new CacheCowHeader()
			                     	{
			                     		CacheValidationApplied = true,
			                     		DidNotExist = false
			                     	};

			var s = cacheCowHeader.ToString();
			Console.WriteLine(s);
			Assert.IsTrue(s.StartsWith(_version));
			Assert.IsTrue(s.IndexOf(CacheCowHeader.ExtensionNames.CacheValidationApplied + "=true") > 0);
			Assert.IsTrue(s.IndexOf(CacheCowHeader.ExtensionNames.DidNotExist + "=false") > 0);

		}
	}
}
