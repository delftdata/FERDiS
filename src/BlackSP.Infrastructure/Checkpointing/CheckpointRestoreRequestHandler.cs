using BlackSP.Core;
using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Checkpointing
{
    public class CheckpointRestoreRequestHandler : IHandler<ControlMessage>
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

        public async Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            if (!message.TryGetPayload<CheckpointRestoreRequestPayload>(out var payload))
            {   //payload not found --> forward message
                return message.Yield();
            }

            Guid checkpointId = payload.CheckpointId;
            _logger.Information($"{_vertexConfiguration.InstanceName} - Restoring checkpoint {checkpointId}");
            await _checkpointService.RestoreCheckpoint(checkpointId).ConfigureAwait(false);
            _logger.Information($"{_vertexConfiguration.InstanceName} - Restored checkpoint {checkpointId}");

            var msg = new ControlMessage();
            msg.AddPayload(new CheckpointRestoreCompletionPayload() { 
                InstanceName = _vertexConfiguration.InstanceName, 
                CheckpointId = checkpointId 
            });
            return new List<ControlMessage>() { msg }.AsEnumerable();
        }
    }
}
