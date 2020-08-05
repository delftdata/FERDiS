using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Streams.Exceptions
{
    [System.Serializable]
    public class ReadMessageFromStreamException : Exception
    {
        public ReadMessageFromStreamException() { }
        public ReadMessageFromStreamException(string message) : base(message) { }
        public ReadMessageFromStreamException(string message, Exception inner) : base(message, inner) { }
        protected ReadMessageFromStreamException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
