using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Handlers
{
    public class CheckpointRestoreRequestHandler : ForwardingPayloadHandlerBase<ControlMessage, CheckpointRestoreRequestPayload>
    {
        private readonly ICheckpointService _checkpointService;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;

        public CheckpointRestoreRequestHandler(ICheckpointService checkpointService, IVertexConfiguration vertexConfiguration, ILogger logger)
        {
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<IEnumerable<ControlMessage>> Handle(CheckpointRestoreRequestPayload payload, CancellationToken t)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));
            
            Guid checkpointId = payload.CheckpointId;
            _logger.Information($"Restoring checkpoint {checkpointId}");
            await _checkpointService.RestoreCheckpoint(checkpointId).ConfigureAwait(false);
            _logger.Information($"Restored checkpoint {checkpointId}");

            var msg = new ControlMessage();
            msg.AddPayload(new CheckpointRestoreCompletionPayload()
            {
                InstanceName = _vertexConfiguration.InstanceName,
                CheckpointId = checkpointId
            });
            return msg.Yield();
        }
    }
}
