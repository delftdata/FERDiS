using BlackSP.Core;
using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Core.Middlewares
{
    public class CheckpointRestoreMiddleware : IMiddleware<ControlMessage>
    {
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;

        public CheckpointRestoreMiddleware(IVertexConfiguration vertexConfiguration, ILogger logger)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            if (!message.TryGetPayload<CheckpointRestoreRequestPayload>(out var payload))
            {
                //forward message
                return new List<ControlMessage>() { message }.AsEnumerable();
            }

            if (!payload.InstanceCheckpointMap.ContainsKey(_vertexConfiguration.InstanceName))
            {
                //checkpoint restore not targetting current vertex;
                return Enumerable.Empty<ControlMessage>();
            }

            Guid checkpointId = payload.InstanceCheckpointMap[_vertexConfiguration.InstanceName];
            _logger.Information($"{_vertexConfiguration.InstanceName} - Restoring checkpoint {checkpointId} (fake)");
            await Task.Delay(5000).ConfigureAwait(false); 
            //TODO: restore actual checkpoint
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
