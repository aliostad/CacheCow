using System;
using System.Collections.Generic;
using System.Text;

namespace CacheCow.Common
{
    public interface ITimedEntityTagProvider<T>
    {
        TimedEntityTagHeaderValue Get(T t);
    }
}
