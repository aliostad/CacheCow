using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Server
{
    public class CacheCowServerException : Exception
    {
        public CacheCowServerException() : base()
        {
            
        }

        public CacheCowServerException(string message)
            : base(message)
        {

        }

        public CacheCowServerException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

    }
}
