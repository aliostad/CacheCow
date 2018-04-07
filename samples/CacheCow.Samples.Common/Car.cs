using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Samples.Common
{
    public class Car
    {
        public int Year { get; set; }

        public string NumberPlate { get; set; }

        public int Id { get; set; }

        public DateTimeOffset LastModified { get; set; }
    }
}
