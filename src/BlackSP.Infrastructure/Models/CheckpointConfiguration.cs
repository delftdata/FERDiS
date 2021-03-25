using BlackSP.Checkpointing;
using BlackSP.Kernel.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Models
{
    [Serializable]
    public class CheckpointConfiguration : ICheckpointConfiguration
    {
        public bool AllowReusingState { get; set; }

        public CheckpointCoordinationMode CoordinationMode { get; set; }

        public int CheckpointIntervalSeconds { get; set; }

        public CheckpointConfiguration(CheckpointCoordinationMode mode, bool allowReusingState, int checkpointIntervalSeconds)
        {
            CoordinationMode = mode;
            AllowReusingState = allowReusingState;
            CheckpointIntervalSeconds = checkpointIntervalSeconds;
        }
    }
}
