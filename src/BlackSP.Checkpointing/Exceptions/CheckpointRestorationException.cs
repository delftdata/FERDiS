using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.Exceptions
{
    [System.Serializable]
    public class CheckpointRestorationException : Exception
    {
        public CheckpointRestorationException() { }
        public CheckpointRestorationException(string message) : base(message) { }
        public CheckpointRestorationException(string message, Exception inner) : base(message, inner) { }
        protected CheckpointRestorationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
