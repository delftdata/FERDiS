using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Checkpointing
{
    public interface ICheckpointService
    {
        bool Register(object o);

        Guid TakeCheckpoint();

        void RestoreCheckpoint(Guid checkpointId);
    }
}
