using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server
{
    /// <summary>
    /// Serialises an object to a byte array
    /// </summary>
    public interface ISerialiser
    {
        /// <summary>
        /// Serialises an object to a byte array
        /// </summary>
        /// <param name="o">object</param>
        /// <returns>buffer</returns>
        byte[] Serialise(object o);
    }
}
