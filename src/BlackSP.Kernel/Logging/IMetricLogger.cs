using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Logging
{
    public interface IMetricLogger
    {

        ILogger GetDefaultLogger();

        /// <summary>
        /// Logs checkpointing metrics
        /// </summary>
        /// <param name="bytes">size of the checkpoint</param>
        /// <param name="time">time it took to take the checkpoint</param>
        /// <param name="wasForced">wether the checkpoint was forced</param>
        void Checkpoint(long bytes, TimeSpan time, bool wasForced);

        /// <summary>
        /// Logs recovery metrics
        /// </summary>
        /// <param name="time">the time it took to restore</param>
        /// <param name="distance">the distance the state went back in time</param>
        void Recovery(TimeSpan time, TimeSpan distance);
    }
}
