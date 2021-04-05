using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Kernel.Checkpointing
{
    public delegate void BeforeCheckpointEvent();
    public delegate void AfterCheckpointEvent(Guid checkpointId);

    public interface ICheckpointService
    {
        event BeforeCheckpointEvent BeforeCheckpointTaken;
        event AfterCheckpointEvent AfterCheckpointTaken;
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
        Task<Guid> TakeCheckpoint(string currentInstanceName, bool isForced = false);

        /// <summary>
        /// Gets the most recently taken checkpoint's id
        /// </summary>
        /// <param name="currentInstanceName"></param>
        /// <returns></returns>
        Guid GetLastCheckpointId(string currentInstanceName);

        /// <summary>
        /// Gets the second most recently taken checkpoint's id
        /// </summary>
        /// <param name="currentInstanceName"></param>
        /// <returns></returns>
        Guid GetSecondLastCheckpointId(string currentInstanceName);

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

        /// <summary>
        /// Deletes any checkpoints from storage that are considered garbage with respect to the provided recovery line
        /// </summary>
        /// <param name="recoveryLine"></param>
        /// <returns></returns>
        Task<int> CollectGarbageAfterRecoveryLine(IRecoveryLine recoveryLine);

        /// <summary>
        /// Empties checkpoint storage, typically most useful on startup/shutdown
        /// </summary>
        /// <returns></returns>
        Task ClearCheckpointStorage();
    }
}
