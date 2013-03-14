using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CacheCow.Server.EntityTagStore.Memcached.Tests
{

    [TestFixture]
    public class LengthedPrefixedStringTests
    {

        [Test]
        public void Test()
        {

            const string TheString = "Ali - ostad **&^%$£";
            var lengthedPerfixedString = new LengthedPrefixedString(TheString);
            byte[] bytes = lengthedPerfixedString.ToByteArray();

            for (int i = 0; i < LengthedPrefixedString.Header.Length; i++)
            {
                Assert.AreEqual(LengthedPrefixedString.Header[i], bytes[i], 
                    string.Format("byte different at position {0}",i ));
            }
            
            var length = BitConverter.ToInt32(bytes, LengthedPrefixedString.Header.Length);

            var s = Encoding.UTF8.GetString(bytes, 4 + LengthedPrefixedString.Header.Length, length);

            Assert.AreEqual(TheString, s);
            Assert.AreEqual(0, bytes[bytes.Length - 1]);
            Assert.AreEqual(0, bytes[bytes.Length - 2]);


        }
    }
}
