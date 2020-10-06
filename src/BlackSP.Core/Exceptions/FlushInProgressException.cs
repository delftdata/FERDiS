using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Exceptions
{
    public class FlushInProgressException : Exception
    {
        public FlushInProgressException()
        {
        }

        public FlushInProgressException(string message) : base(message)
        {
        }

        public FlushInProgressException(string message, Exception innerException) : base(message, innerException)
        {
        }

    }
}
