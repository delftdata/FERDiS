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
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ICheckpointConfiguration _checkpointConfiguration;
        private readonly ICheckpointService _checkpointService;
        private readonly ILogger _logger;

        private DateTime _lastCheckpointUtc;
        private TimeSpan _checkpointInterval;

        public UncoordinatedProtocol(ICheckpointService checkpointService, ICheckpointConfiguration checkpointConfiguration, ILogger logger)
        {
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _checkpointConfiguration = checkpointConfiguration ?? throw new ArgumentNullException(nameof(checkpointConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _checkpointInterval = TimeSpan.FromSeconds(_checkpointConfiguration.CheckpointIntervalSeconds);
        }

        /// <summary>
        /// Checks local checkpoint condition (does not take actual checkpoint)</br>
        /// Make sure to update last checkpoint utc after taking a checkpoint
        /// </summary>
        /// <returns>Guid of new checkpoint or Guid.Empty if no checkpoint was taken</returns>
        public async Task<bool> CheckCheckpointCondition(DateTime processingTime)
        {
            if (_lastCheckpointUtc == default)
            {
                _lastCheckpointUtc = DateTime.UtcNow;
            }
            return processingTime - _lastCheckpointUtc > _checkpointInterval;
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
