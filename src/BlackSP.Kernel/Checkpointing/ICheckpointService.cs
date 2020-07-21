using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Checkpointing
{
    public interface ICheckpointService
    {
        bool Register(object o);

        byte[] Checkpoint();

        void Restore(byte[] checkpoint);
    }
}
