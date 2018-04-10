using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server
{
    /// <summary>
    /// Can produce hashes for byte arrays
    /// </summary>
    public interface IHasher : IDisposable
    {
        /// <summary>
        /// Computes hash
        /// </summary>
        /// <param name="bytes">buffer (most likley serialised representation of an object)</param>
        /// <returns>hash in base64 encoding</returns>
        string ComputeHash(byte[] bytes);
    }
}
