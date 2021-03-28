﻿using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    public class CommunicationInducedCheckpointingHandler : ForwardingPayloadHandlerBase<DataMessage, CICPayload>
    {

        private readonly ICheckpointService _checkpointingService;
        private readonly IVertexConfiguration _vertexConfiguration;

        public CommunicationInducedCheckpointingHandler(IReceiverSource<DataMessage> messageSource, ICheckpointService checkpointingService, IVertexConfiguration vertexConfiguration)
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
