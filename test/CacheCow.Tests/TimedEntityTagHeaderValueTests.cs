using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CacheCow.Common;
using Xunit;

namespace CacheCow.Tests
{
	public static class TimedEntityTagHeaderValueTests
	{
        [Theory]
		[InlineData("\"1234567\"", false)]
		[InlineData("\"1234567\"", true)]
		public static void ToStringAndTryParseTest(string tag, bool isWeak)
		{
			var headerValue = new TimedEntityTagHeaderValue(tag, isWeak);
			var s = headerValue.ToString();
			TimedEntityTagHeaderValue headerValue2 = null;
			Assert.True(TimedEntityTagHeaderValue.TryParse(s, out headerValue2));
			Assert.Equal(headerValue.Tag, headerValue2.Tag);
			Assert.Equal(headerValue.LastModified.ToString(), headerValue2.LastModified.ToString());
			Assert.Equal(headerValue.IsWeak, headerValue2.IsWeak);
			Assert.Equal(headerValue.ToString(), headerValue2.ToString());
		}
	}
}
