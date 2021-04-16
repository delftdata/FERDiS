using BlackSP.Core.Coordination;
using BlackSP.Core.Extensions;
using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Handlers
{
    /// <summary>
    /// ControlMessage handler that handles responses of worker requests on the coordinator side
    /// </summary>
    public class CheckpointTakenHandler : ForwardingPayloadHandlerBase<ControlMessage, CheckpointTakenPayload>
    {

        private readonly MessageLoggingSequenceManager _sequenceNrManager;
        private readonly ICheckpointService _checkpointService;
        private readonly IVertexGraphConfiguration _graphConfiguration;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;
        public CheckpointTakenHandler(MessageLoggingSequenceManager sequenceNrManager, 
            ICheckpointService checkpointService,
            IVertexGraphConfiguration graphConfiguration,
            IVertexConfiguration vertexConfiguration,
            ILogger logger)
        {
            _sequenceNrManager = sequenceNrManager ?? throw new ArgumentNullException(nameof(sequenceNrManager));
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _graphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<IEnumerable<ControlMessage>> Handle(CheckpointTakenPayload payload)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));
            
            //verbose
            _logger.Fatal("Handling CheckpointTakenPayload from " + payload.OriginInstance + " with checkpoint " + payload.CheckpointId);

            if(payload.AssociatedSequenceNumbers != null)
            {
                //update sequencenr cache
                _sequenceNrManager.AddCheckpoint(payload.CheckpointId, payload.AssociatedSequenceNumbers);
            }

            //first determine recovery line under worst case failure scenario (everything fails)
            var rl = await _checkpointService.CalculateRecoveryLine(_graphConfiguration.InstanceNames).ConfigureAwait(false);

            var output = new List<ControlMessage>();
            foreach(var entry in rl.RecoveryMap)
            {
                var instanceName = entry.Key;
                var cpId = entry.Value;

                var pruneDict = _sequenceNrManager.GetPrunableSequenceNumbers(cpId);
                foreach(var pruneEntry in pruneDict)
                {
                    var seqnr = pruneEntry.Value;
                    if(seqnr == -1)
                    {
                        continue;
                    }

                    var pruneRequest = new LogPruneRequestPayload { InstanceName = instanceName, SequenceNumber = seqnr };
                    var msg = new ControlMessage(_vertexConfiguration.GetPartitionKeyForInstanceName(pruneEntry.Key));
                    msg.AddPayload(pruneRequest);
                    output.Add(msg);
                }

            }

            //TODO: GC?
            
            return AssociatedMessage.Yield().Concat(output);
        }

    }
}
