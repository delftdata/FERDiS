using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel
{
    /// <summary>
    /// A core processing element responsible for operating on messages
    /// </summary>
    public interface IProcessManager
    {
        //void SwitchMode(ProcessorState newState);
    }

    public enum ProcessorState
    {
        /// <summary>
        /// process control messages only
        /// </summary>
        Passive,
        /// <summary>
        /// process all message types
        /// </summary>
        Active,
    }
}
