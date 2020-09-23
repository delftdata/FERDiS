using BlackSP.Checkpointing;
using BlackSP.Core.Extensions;
using BlackSP.Core.Handlers;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public CheckpointDependencyTrackingDispatchHandler(ICheckpointService checkpointService, 
            IVertexConfiguration vertexConfiguration,
            ICheckpointConfiguration checkpointConfiguration)
        {
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _checkpointConfiguration = checkpointConfiguration ?? throw new ArgumentNullException(nameof(checkpointConfiguration));
        }

        public Task<IEnumerable<DataMessage>> Handle(DataMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            var cpId = _checkpointConfiguration.CoordinationMode == CheckpointCoordinationMode.Coordinated
                ? _checkpointService.GetSecondLastCheckpointId(_vertexConfiguration.InstanceName)
                : _checkpointService.GetLastCheckpointId(_vertexConfiguration.InstanceName);
            
            if(cpId != Guid.Empty)
            {
                var payload = new CheckpointDependencyPayload { CheckpointId = cpId };
                message.AddPayload(payload);
            }
            return Task.FromResult(message.Yield());
        }
    }
}
