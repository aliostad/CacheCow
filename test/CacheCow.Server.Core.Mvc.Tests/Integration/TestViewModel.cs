using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server.Core.Mvc.Tests
{
    public class TestViewModel
    {
        public string Name { get; set; }

        public int Id { get; set; }

        public DateTimeOffset LastModified { get; set; }
    }
}
