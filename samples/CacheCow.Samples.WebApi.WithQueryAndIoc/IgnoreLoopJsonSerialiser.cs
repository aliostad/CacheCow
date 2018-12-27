using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CacheCow.Server;
using Newtonsoft.Json;

namespace CacheCow.Samples.WebApi.WithQueryAndIoc
{
    class IgnoreLoopJsonSerialiser : ISerialiser
    {
        public byte[] Serialise(object o)
        {
            Console.WriteLine("Serialising by IgnoreLoopJsonSerialiser");

            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects
            };

            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(o, settings));
        }
    }
}
