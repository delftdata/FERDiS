using BlackSP.Core.Extensions;
using BlackSP.Core.Handlers;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    public class CommunicationInducedCheckpointingHandler : ForwardingPayloadHandlerBase<DataMessage, CICPayload>
    {

        private readonly ICheckpointService _checkpointingService;
        private readonly IVertexConfiguration _vertexConfiguration;

        public CommunicationInducedCheckpointingHandler(ICheckpointService checkpointingService, IVertexConfiguration vertexConfiguration)
        {
            _checkpointingService = checkpointingService ?? throw new ArgumentNullException(nameof(checkpointingService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
        }


        protected override async Task<IEnumerable<DataMessage>> Handle(CICPayload payload)
        {
            bool condition = false;
            if (condition)
            {
                await _checkpointingService.TakeCheckpoint(_vertexConfiguration.InstanceName).ConfigureAwait(false);
            }

            return AssociatedMessage.Yield();
        }
    }
}
