using BlackSP.Checkpointing.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.Persistence
{
    interface ICheckpointStorage
    {
        void Store(Checkpoint checkpoint);

        Checkpoint Retrieve(Guid id);
    }
}
