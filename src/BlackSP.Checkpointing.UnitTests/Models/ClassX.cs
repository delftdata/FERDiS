using BlackSP.Checkpointing.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlackSP.Checkpointing.UnitTests.Models
{
    class ClassX
    {
        [Checkpointable]
        private BlockingCollection<Stream> streams;//this is a non-serializable type (used to test that this indeed is NOT supported)

        public ClassX()
        {
            streams = new BlockingCollection<Stream>();
        }
    }
}
