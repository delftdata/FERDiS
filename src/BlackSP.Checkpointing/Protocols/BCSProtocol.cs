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
        public bool CheckCheckpointCondition(int mClock)
        {
            if (mClock > lClock)
            {
                lClock = mClock;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the local clock (lc_i)
        /// </summary>
        /// <returns></returns>
        public int GetClockValue()
        {
            return lClock;
        }
    }
}
