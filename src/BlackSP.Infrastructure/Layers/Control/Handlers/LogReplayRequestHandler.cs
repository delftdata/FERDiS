using BlackSP.Core.Coordination;
using BlackSP.Core.Extensions;
using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using BlackSP.Infrastructure.Layers.Data;
using BlackSP.Kernel;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Handlers
{
    /// <summary>
    /// ControlMessage handler that extends worker request payload messages with log replay data (for coordinator)
    /// </summary>
    public class LogReplayRequestHandler : IHandler<ControlMessage>
    {
        private readonly MessageLoggingSequenceManager _messageLogManager;
        private readonly WorkerGraphStateManager _graphStateManager;
        private readonly IPartitioner<ControlMessage> _partitioner;
        private readonly IVertexGraphConfiguration _graphConfiguration;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;

        private IRecoveryLine _lastRestoredRecoveryLine;
        private IEnumerable<ControlMessage> _pendingMessages;

        public LogReplayRequestHandler(
            MessageLoggingSequenceManager messageLogManager,
            WorkerGraphStateManager graphStateManager,
            IPartitioner<ControlMessage> partitioner,
            IVertexGraphConfiguration graphConfiguration,  
            IVertexConfiguration vertexConfiguration,  
            ILogger logger)
        {
            _messageLogManager = messageLogManager ?? throw new ArgumentNullException(nameof(messageLogManager));
            _graphStateManager = graphStateManager ?? throw new ArgumentNullException(nameof(graphStateManager));
            _partitioner = partitioner ?? throw new ArgumentNullException(nameof(partitioner));
            _graphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            RegisterEvents();
        }

        public Task<IEnumerable<ControlMessage>> Handle(ControlMessage message, CancellationToken t)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            if(message.TryExtractPayload<WorkerRequestPayload>(out var requestPayload)) 
            {
                if (requestPayload.RequestType == WorkerRequestType.StartProcessing)
                {
                    if(_lastRestoredRecoveryLine != null) //if null = initial start, else recovery
                    {
                        var targets = _partitioner.Partition(message);
                        if (targets.Count() > 1)
                        {
                            throw new InvalidOperationException($"{this.GetType()} expects start processing requests to NOT be broadcasted.");
                        }
                        var (endpoint, shard) = targets.First();
                        var targetName = endpoint.GetRemoteInstanceName(shard);

                        //determine downstream workers
                        var downstreamNames = _graphConfiguration.GetAllInstancesDownstreamOf(targetName, true);

                        _logger.Debug($"Building LogReplayRequest for {targetName}");

                        var replayDict = new Dictionary<string, int>();
                        foreach(var downstreamName in downstreamNames)
                        {
                            //determine which checkpoints they have restored last
                            //determine sequence numbers associated with those checkpoints
                            //determine sequence numbers belonging to {targetName} in each checkpoint
                            var prunePoints = _messageLogManager.GetPrunableSequenceNumbers(_lastRestoredRecoveryLine.RecoveryMap[downstreamName]);
                            var replayPoint = prunePoints.ContainsKey(targetName) ? prunePoints[targetName] + 1 : 0;
                            replayDict.Add(downstreamName, replayPoint);
                        }                        

                        if(replayDict.Any())
                        {
                            //add replay payload
                            message.AddPayload(new LogReplayRequestPayload { ReplayMap = replayDict });
                            _logger.Information($"Requesting {targetName} to replay its logs - {string.Join(", ", replayDict)}");
                        }
                        
                    }
                }
                message.AddPayload(requestPayload); //re-add payload
            }

            if(_pendingMessages != null)
            {
                var res = Task.FromResult(_pendingMessages.Concat(message.Yield()).ToArray().AsEnumerable());
                _pendingMessages = null;
                return res;
            }
            return Task.FromResult(message.Yield());
        }

        private void RegisterEvents()
        {
            _graphStateManager.OnRecoveryLineRestoreStart += _graphStateManager_OnRecoveryLineRestoreStart;
        }

        private void _graphStateManager_OnRecoveryLineRestoreStart(IRecoveryLine recoveryLine)
        {
            _lastRestoredRecoveryLine = recoveryLine ?? throw new ArgumentNullException(nameof(recoveryLine));
            _pendingMessages = ConstructReplayMessagesForNonRecoveringWorkers(recoveryLine);
        }

        private IEnumerable<ControlMessage> ConstructReplayMessagesForNonRecoveringWorkers(IRecoveryLine recoveryLine)
        {
            foreach (var instanceName in _graphConfiguration.InstanceNames)
            {
                if(recoveryLine.AffectedWorkers.Contains(instanceName) || instanceName == _vertexConfiguration.InstanceName) //the || case excludes the coordinator instance 
                {
                    continue; //we're looking for non-affected workers 
                }

                var replayDict = new Dictionary<string, int>();

                //for each non-affected worker..
                foreach (var downstreamName in _graphConfiguration.GetAllInstancesDownstreamOf(instanceName, true))
                {
                    if (!recoveryLine.AffectedWorkers.Contains(downstreamName))
                    {
                        continue; //we're looking for affected downstreams
                    }
                    //this downstream is recovering --> send replay instruction

                    replayDict.Add(downstreamName, _messageLogManager.GetPrunableSequenceNumbers(recoveryLine.RecoveryMap[downstreamName])[instanceName] + 1);
                }

                if(!replayDict.Any()) //nothing to replay, no need for a log replay request..
                {
                    continue;
                }

                var msg = new ControlMessage(_vertexConfiguration.GetPartitionKeyForInstanceName(instanceName));
                var payload = new LogReplayRequestPayload { ReplayMap = replayDict };
                _logger.Information($"Requesting {instanceName} to replay its logs - {string.Join(", ", replayDict)} (prepared)");
                msg.AddPayload(payload);
                yield return msg;
            }
        }
    }
}
