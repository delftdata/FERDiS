﻿using BlackSP.Checkpointing;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    /// <summary>
    /// retrieves latest taken checkpoint id and sends it along with outgoing messages
    /// </summary>
    public class CheckpointDependencyTrackingDispatchHandler : IHandler<DataMessage>
    {

        private readonly ICheckpointService _checkpointService;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ICheckpointConfiguration _checkpointConfiguration;

        private Guid _lastSentCheckpointId;

        public CheckpointDependencyTrackingDispatchHandler(ICheckpointService checkpointService, 
            IVertexConfiguration vertexConfiguration,
            ICheckpointConfiguration checkpointConfiguration)
        {
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _checkpointConfiguration = checkpointConfiguration ?? throw new ArgumentNullException(nameof(checkpointConfiguration));
        }

        public Task<IEnumerable<DataMessage>> Handle(DataMessage message, CancellationToken t)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));


            var cpId = _checkpointConfiguration.CoordinationMode == CheckpointCoordinationMode.Coordinated
                ? _checkpointService.GetSecondLastCheckpointId(_vertexConfiguration.InstanceName)
                : _checkpointService.GetLastCheckpointId(_vertexConfiguration.InstanceName);
            
            if(cpId != Guid.Empty && _lastSentCheckpointId != cpId)
            {
                var payload = new CheckpointDependencyPayload { CheckpointId = cpId };
                message.AddPayload(payload);
                _lastSentCheckpointId = cpId;
            }
            return Task.FromResult(message.Yield());
        }
    }
}
