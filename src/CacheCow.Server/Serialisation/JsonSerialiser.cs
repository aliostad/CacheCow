using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server
{
    /// <summary>
    /// Json impl
    /// </summary>
    public class JsonSerialiser : ISerialiser
    {
        public byte[] Serialise(object o)
        {
            if (o == null)
                throw new ArgumentNullException("o");

            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(o));
        }
    }
}
