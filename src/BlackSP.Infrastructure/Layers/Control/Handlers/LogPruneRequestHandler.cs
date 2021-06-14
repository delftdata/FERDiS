using BlackSP.Checkpointing.Persistence;
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
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Handlers
{
    /// <summary>
    /// ControlMessage handler that handles responses of worker requests on the coordinator side
    /// </summary>
    public class LogPruneRequestHandler : ForwardingPayloadHandlerBase<ControlMessage, CheckpointTakenPayload>
    {

        private readonly MessageLoggingSequenceManager _sequenceNrManager;
        private readonly WorkerGraphStateManager _graphStateManager;
        private readonly ICheckpointService _checkpointService;
        private readonly ICheckpointStorage _checkpointStorage;
        private readonly IVertexGraphConfiguration _graphConfiguration;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;

        private IDictionary<string, IDictionary<string, int>> _lastRequestedPrune;

        public LogPruneRequestHandler(MessageLoggingSequenceManager sequenceNrManager,
            WorkerGraphStateManager graphStateManager,
            ICheckpointService checkpointService,
            ICheckpointStorage checkpointStorage,
            IVertexGraphConfiguration graphConfiguration,
            IVertexConfiguration vertexConfiguration,
            ILogger logger)
        {
            _sequenceNrManager = sequenceNrManager ?? throw new ArgumentNullException(nameof(sequenceNrManager));
            _graphStateManager = graphStateManager ?? throw new ArgumentNullException(nameof(graphStateManager));
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _checkpointStorage = checkpointStorage ?? throw new ArgumentNullException(nameof(checkpointStorage));
            _graphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _lastRequestedPrune = new Dictionary<string, IDictionary<string, int>>();
            foreach(var instance in _graphConfiguration.InstanceNames)
            {
                _lastRequestedPrune.Add(instance, new Dictionary<string, int>());
            }
        }

        protected override async Task<IEnumerable<ControlMessage>> Handle(CheckpointTakenPayload payload, CancellationToken t)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));
            
            _logger.Debug("Handling CheckpointTakenPayload from " + payload.OriginInstance + " with checkpoint " + payload.CheckpointId);

            if(payload.MetaData == null)
            {
                _logger.Fatal("Received checkpoint taken payload without metadata");
            }
            
            if(payload.MetaData.Dependencies == null)
            {
                payload.MetaData.Dependencies = new Dictionary<string, Guid>();
            }

            _checkpointStorage.AddMetaData(payload.MetaData);

            if(payload.AssociatedSequenceNumbers != null)
            {
                //update sequencenr cache
                _sequenceNrManager.AddCheckpoint(payload.CheckpointId, payload.AssociatedSequenceNumbers);
            }

            if(_graphStateManager.CurrentState != WorkerGraphStateManager.State.Running)
            {
                return AssociatedMessage.Yield(); //this corner-case ensures no messages get pruned during failure-recovery
            }

            //first determine recovery line under worst case failure scenario (everything fails)
            var rl = await _checkpointService.CalculateRecoveryLine(_graphConfiguration.InstanceNames).ConfigureAwait(false);

            var output = new List<ControlMessage>();
            foreach(var entry in rl.RecoveryMap)
            {
                var instanceName = entry.Key;
                var cpId = entry.Value;

                var pruneDict = _sequenceNrManager.GetPrunableSequenceNumbers(cpId);
                var lastRequestedPrune = _lastRequestedPrune[instanceName];
                foreach(var pruneEntry in pruneDict)
                {
                    var targetName = pruneEntry.Key;
                    if(!lastRequestedPrune.ContainsKey(targetName))
                    {
                        lastRequestedPrune.Add(targetName, -1);
                    }

                    var seqnr = pruneEntry.Value;
                    if(seqnr == -1 || lastRequestedPrune[targetName] == seqnr)
                    {
                        continue;
                    }
                    
                    lastRequestedPrune[targetName] = seqnr;

                    var pruneRequest = new LogPruneRequestPayload { InstanceName = instanceName, SequenceNumber = seqnr };
                    var msg = new ControlMessage(_vertexConfiguration.GetPartitionKeyForInstanceName(pruneEntry.Key));
                    _logger.Information($"Requesting {targetName} to prune a log: [{instanceName}, {seqnr}]");

                    msg.AddPayload(pruneRequest);
                    output.Add(msg);
                }

            }

            //TODO: GC?
            
            return AssociatedMessage.Yield().Concat(output);
        }

    }
}
