using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CacheCow.Server
{
    /// <summary>
    /// Sha1 impl
    /// </summary>
    public class Sha1Hasher : IHasher
    {
        private SHA256CryptoServiceProvider _sha1 = new SHA256CryptoServiceProvider();       

        public string ComputeHash(byte[] bytes)
        {
            return Convert.ToBase64String(_sha1.ComputeHash(bytes));
        }

        public void Dispose()
        {
            _sha1.Dispose();
        }
    }
}
