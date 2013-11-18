using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Tests.Server.Integration.MiniServer
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public static Dictionary<int, Item> Items = new Dictionary<int, Item>();
    }
}
