using BlackSP.Checkpointing.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Checkpointing.Persistence
{
    public interface ICheckpointStorage
    {
        Task Store(Checkpoint checkpoint);

        Task<Checkpoint> Retrieve(Guid id);

        Task Delete(Guid id);
    }
}
