using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Exceptions
{
    public class ReceptionCancelledException : Exception
    {
        public ReceptionCancelledException()
        {
        }

        public ReceptionCancelledException(string message) : base(message)
        {
        }

        public ReceptionCancelledException(string message, Exception innerException) : base(message, innerException)
        {
        }

    }
}
