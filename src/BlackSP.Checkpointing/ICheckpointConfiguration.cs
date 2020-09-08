using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing
{
    public enum CheckpointCoordinationMode
    {
        Uncoordinated,
        Coordinated,
        CommunicationInduced
    }

    /// <summary>
    /// Model class for checkpoint taking and restoring configuration
    /// </summary>
    public interface ICheckpointConfiguration
    {
        /// <summary>
        /// Configures recovery coordination to consider existing state as valid to reuse. This can 
        /// introduce in-transit messages to a failed instance to get lost. <br/>
        /// 
        /// </summary>
        bool AllowReusingState { get; }

        /// <summary>
        /// Marks the mode of checkpoint coordination that each compute instance should obey
        /// </summary>
        CheckpointCoordinationMode CoordinationMode { get; }

    }
}
