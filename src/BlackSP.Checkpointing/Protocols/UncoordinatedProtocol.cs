using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Checkpointing.Protocols
{
    public class UncoordinatedProtocol
    {
        /// <summary>
        /// Autofac factory for instances
        /// </summary>
        /// <param name="interval"></param>
        /// <returns></returns>
        public delegate UncoordinatedProtocol Factory(TimeSpan interval, DateTime startFrom);

        private readonly ILogger _logger;

        private DateTime _lastCheckpointUtc;
        private TimeSpan _checkpointInterval;

        public UncoordinatedProtocol(TimeSpan interval, DateTime startFrom, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _checkpointInterval = interval;
            _lastCheckpointUtc = startFrom;
        }

        /// <summary>
        /// Checks local checkpoint condition (does not take actual checkpoint)</br>
        /// Make sure to update last checkpoint utc after taking a checkpoint
        /// </summary>
        /// <returns>Guid of new checkpoint or Guid.Empty if no checkpoint was taken</returns>
        public bool CheckCheckpointCondition(DateTime processingTime)
        {
            if (_lastCheckpointUtc == default)
            {
                _lastCheckpointUtc = DateTime.UtcNow;
            }
            return processingTime - _lastCheckpointUtc >= _checkpointInterval;
        }

        public void SetLastCheckpointUtc(DateTime lastCheckpointUtc)
        {
            if(lastCheckpointUtc == default)
            {
                throw new ArgumentException("default date provided to overwrite last checkpoint moment", nameof(lastCheckpointUtc));
            }
            _lastCheckpointUtc = lastCheckpointUtc;
        }

    }
}
