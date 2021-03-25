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
    public class BCSProtocol
    {

        private readonly ICheckpointService _checkpointService;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly UncoordinatedProtocol _baseProtocol;
        private readonly ILogger _logger;
        
        /// <summary>
        /// Local Clock value lc_i<br/>
        /// Part of state to ensure correct resumption of protocol post-recovery
        /// </summary>
        [ApplicationState]
        private int lClock;

        public BCSProtocol(ICheckpointService checkpointService, 
            IVertexConfiguration vertexConfiguration, 
            UncoordinatedProtocol baseProtocol,
            ILogger logger)
        {
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _baseProtocol = baseProtocol ?? throw new ArgumentNullException(nameof(baseProtocol));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            lClock = 0;
        }

        /// <summary>
        /// Handle reception of a message clock (m.lc) value from a neighbouring instance
        /// </summary>
        /// <param name="mClock"></param>
        /// <returns></returns>
        public async Task CheckCheckpointCondition(int mClock, DateTime processingTime)
        {
            if(mClock > lClock)
            {
                lClock = mClock;
                await TakeForcedCheckpoint().ConfigureAwait(false);
                _baseProtocol.OverrideLastCheckpointUtc(processingTime);
            }
            else
            {
                var cp = await _baseProtocol.CheckCheckpointCondition(processingTime).ConfigureAwait(false);
                if (cp != Guid.Empty) //check if a checkpoint was taken locally
                {
                    lClock++;
                }
            }
        }

        /// <summary>
        /// Returns the local clock (lc_i)
        /// </summary>
        /// <returns></returns>
        public int GetClockValue()
        {
            return lClock;
        }

        /// <summary>
        /// Utility
        /// </summary>
        /// <returns></returns>
        private async Task TakeForcedCheckpoint()
        {
            _logger.Information($"Forced checkpoint will be taken");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var cpId = await _checkpointService.TakeCheckpoint(_vertexConfiguration.InstanceName).ConfigureAwait(false);
            sw.Stop();
            _logger.Information($"Forced checkpoint {cpId} has been taken in {sw.ElapsedMilliseconds}ms");
        }
    }
}
