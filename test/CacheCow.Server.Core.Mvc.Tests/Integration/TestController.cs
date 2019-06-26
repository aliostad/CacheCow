﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server.Core.Mvc.Tests
{
    public class TestController : Controller
    {
        [HttpCacheFactory]
        public TestViewModel Get(int id)
        {
            if (id == 42)
                throw new MeaningOfLifeException();

            return new TestViewModel()
            {
                Id = id,
                Name = "JLH",
                LastModified = new DateTimeOffset(2018, 04, 01, 0, 0, 0, TimeSpan.FromHours(1))
            };
        }
    }

    public class MeaningOfLifeException : Exception
    {

    }

}
