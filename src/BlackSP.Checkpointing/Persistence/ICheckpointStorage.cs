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
        Task<IEnumerable<MetaData>> GetAllMetaData(bool forcePull = false);

        void AddMetaData(MetaData meta);

        MetaData GetMetaData(Guid id);

        /// <summary>
        /// Stores a checkpoint in the storage
        /// </summary>
        /// <param name="checkpoint"></param>
        /// <returns>byte size of the checkpoint</returns>
        Task<long> Store(Checkpoint checkpoint);

        Task<Checkpoint> Retrieve(Guid id);

        Task Delete(Guid id);
    }
}
