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
        /// Checks local checkpoint condition and if satisfied takes a checkpoint. 
        /// </summary>
        /// <returns>Guid of new checkpoint or Guid.Empty if no checkpoint was taken</returns>
        public async Task<Guid> CheckCheckpointCondition(DateTime processingTime)
        {
            if (_lastCheckpointUtc == default)
            {
                _lastCheckpointUtc = DateTime.UtcNow;
            }

            if (processingTime - _lastCheckpointUtc > _checkpointInterval)
            {
                _logger.Information($"Uncoordinated checkpoint will be taken, configured interval is {_checkpointInterval.TotalSeconds}s");
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var cpId = await _checkpointService.TakeCheckpoint(_vertexConfiguration.InstanceName).ConfigureAwait(false);
                sw.Stop();
                _logger.Information($"Checkpoint {cpId} has been taken in {sw.ElapsedMilliseconds}ms");
                _lastCheckpointUtc = processingTime;
                return cpId;
            }
            return Guid.Empty;
        }

        public void OverrideLastCheckpointUtc(DateTime lastCheckpointUtc)
        {
            if(lastCheckpointUtc == default)
            {
                throw new ArgumentException("default date provided to overwrite last checkpoint moment", nameof(lastCheckpointUtc));
            }
            _lastCheckpointUtc = lastCheckpointUtc;
        }

    }
}
