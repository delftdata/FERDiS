using BlackSP.Core.Coordination;
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
    /// ControlMessage handler that extends worker request payload messages with log replay data
    /// </summary>
    public class LogReplayRequestHandler : IHandler<ControlMessage>
    {
        private readonly MessageLoggingSequenceManager _messageLogManager;
        private readonly WorkerGraphStateManager _graphStateManager;
        private readonly IPartitioner<ControlMessage> _partitioner;
        private readonly IVertexGraphConfiguration _graphConfiguration;
        private readonly ILogger _logger;

        private IRecoveryLine _lastRestoredRecoveryLine;

        public LogReplayRequestHandler(
            MessageLoggingSequenceManager messageLogManager,
            WorkerGraphStateManager graphStateManager,
            IPartitioner<ControlMessage> partitioner,
            IVertexGraphConfiguration graphConfiguration,  
            ILogger logger)
        {
            _messageLogManager = messageLogManager ?? throw new ArgumentNullException(nameof(messageLogManager));
            _graphStateManager = graphStateManager ?? throw new ArgumentNullException(nameof(graphStateManager));
            _partitioner = partitioner ?? throw new ArgumentNullException(nameof(partitioner));
            _graphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            RegisterEvents();
        }

        public Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
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

                        //Verbose
                        _logger.Fatal($"Building LogReplayRequest for {targetName}");

                        var replayDict = new Dictionary<string, int>();
                        foreach(var downstreamName in downstreamNames)
                        {
                            //determine which checkpoints they have restored last
                            //determine sequence numbers associated with those checkpoints
                            //determine sequence numbers belonging to {targetName} in each checkpoint
                            var replayPoint = _messageLogManager.GetPrunableSequenceNumbers(_lastRestoredRecoveryLine.RecoveryMap[downstreamName])[targetName];
                            replayDict.Add(downstreamName, replayPoint);
                        }

                        if(replayDict.Any())
                        {
                            //add replay payload
                            message.AddPayload(new LogReplayRequestPayload { ReplayMap = replayDict });
                            //Verbose
                            _logger.Fatal($"Did build LogReplayRequest for {targetName} - {string.Join(", ", replayDict)}");
                        }
                        
                    }
                }
                message.AddPayload(requestPayload); //re-add payload
            }

            /*
            if (message.TryExtractPayload<CheckpointRestoreRequestPayload>(out var restorePayload))
            {
                var cpId = restorePayload.CheckpointId; //TODO: expect replay
                //HOL UP : replay automatically expected due to state overwriting?? (NOT SURE?)
                message.AddPayload(restorePayload); //re-add payload
            }
            */
            return Task.FromResult(message.Yield());
        }

        private void RegisterEvents()
        {
            _graphStateManager.OnRecoveryLineRestoreStart += _graphStateManager_OnRecoveryLineRestoreStart;
        }

        private void _graphStateManager_OnRecoveryLineRestoreStart(IRecoveryLine recoveryLine)
        {
            _lastRestoredRecoveryLine = recoveryLine ?? throw new ArgumentNullException(nameof(recoveryLine));
        }
    }
}
