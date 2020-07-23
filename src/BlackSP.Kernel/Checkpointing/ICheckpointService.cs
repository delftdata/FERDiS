using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Kernel.Checkpointing
{
    public interface ICheckpointService
    {

        /// <summary>
        /// Register an [Checkpointable] annotated class instance, will track registered object and include it in checkpoint creation and restoration<br/>
        /// Will ignore registrations with non-annotated class instances;
        /// </summary>
        /// <param name="o"></param>
        bool RegisterObject(object o);

        /// <summary>
        /// Updates which checkpoints are dependent on. information is used in storage, retrieval and recovery
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="checkpointId"></param>
        void UpdateCheckpointDependency(string origin, Guid checkpointId);

        /// <summary>
        /// Take a checkpoint, returns an ID useable to restore said checkpoint
        /// </summary>
        /// <returns></returns>
        Task<Guid> TakeCheckpoint();

        /// <summary>
        /// Restore a checkpoint, fails when there is a discrepancy between the objects registered and the objects in the checkpoint
        /// </summary>
        /// <param name="checkpointBytes"></param>
        /// <returns></returns>
        Task RestoreCheckpoint(Guid checkpointId);
    }
}
