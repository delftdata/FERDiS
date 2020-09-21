using BlackSP.Kernel.Models;
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
        Task<Guid> TakeCheckpoint(string currentInstanceName);

        /// <summary>
        /// Will attempt to take a checkpoint of the application's initial state. 
        /// If this checkpoint was already taken and the vertex is recovering from a failure, no checkpoint will be taken
        /// </summary>
        /// <returns></returns>
        Task TakeInitialCheckpointIfNotExists(string currentInstanceName);

        /// <summary>
        /// Restore a checkpoint, fails when there is a discrepancy between the objects registered and the objects in the checkpoint
        /// </summary>
        /// <param name="checkpointBytes"></param>
        /// <returns></returns>
        Task RestoreCheckpoint(Guid checkpointId);

        /// <summary>
        /// Determines recovery line under failure assumptions
        /// </summary>
        /// <param name="allowReusingExistingState">
        /// Indicates if calculation should atempt to keep running instances running with their existing internal state.
        /// </param>
        /// <param name="failedInstanceNames"></param>
        /// <returns></returns>
        Task<IRecoveryLine> CalculateRecoveryLine(IEnumerable<string> failedInstanceNames);
    }
}
