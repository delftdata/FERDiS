using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.Exceptions
{
    [System.Serializable]
    public class CheckpointingPreconditionException : Exception
    {
        public CheckpointingPreconditionException() { }
        public CheckpointingPreconditionException(string message) : base(message) { }
        public CheckpointingPreconditionException(string message, Exception inner) : base(message, inner) { }
        protected CheckpointingPreconditionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
