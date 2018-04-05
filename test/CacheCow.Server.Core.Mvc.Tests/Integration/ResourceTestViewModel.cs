using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Server.Core.Mvc.Tests
{
    public class ResourceTestViewModel : TestViewModel, ICacheResource
    {
        public TimedEntityTagHeaderValue GetTimedETag()
        {
            return new TimedEntityTagHeaderValue(this.LastModified);
        }
    }
}
