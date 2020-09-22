using BlackSP.Checkpointing.Core;
using BlackSP.Checkpointing.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Checkpointing.Persistence
{
    public interface ICheckpointStorage
    {
        Task<IEnumerable<MetaData>> GetAllMetaData();

        Task Store(Checkpoint checkpoint);

        Task<Checkpoint> Retrieve(Guid id);

        Task Delete(Guid id);
    }
}
