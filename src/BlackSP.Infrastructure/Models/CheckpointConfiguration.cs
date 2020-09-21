using BlackSP.Checkpointing;
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

        public CheckpointConfiguration(CheckpointCoordinationMode mode, bool allowReusingState)
        {
            CoordinationMode = mode;
            AllowReusingState = allowReusingState;
        }
    }
}
