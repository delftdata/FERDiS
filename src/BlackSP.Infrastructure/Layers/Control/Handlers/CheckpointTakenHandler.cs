using BlackSP.Core.Coordination;
using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using BlackSP.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Handlers
{
    /// <summary>
    /// ControlMessage handler that handles responses of worker requests on the coordinator side
    /// </summary>
    public class CheckpointTakenHandler : ForwardingPayloadHandlerBase<ControlMessage, CheckpointTakenPayload>
    {

        private readonly WorkerGraphStateManager _stateManager;
        public CheckpointTakenHandler(WorkerGraphStateManager graphStateManager)
        {
            _stateManager = graphStateManager ?? throw new ArgumentNullException(nameof(graphStateManager));
        }

        protected override Task<IEnumerable<ControlMessage>> Handle(CheckpointTakenPayload payload)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));
            
            //TODO: UPDATE LOCAL STATE WITH NEW CHECKPOINT AND ASSOCIATED SEQUENCE NRS
            //      DO RECOVERY LINE CALC
            //      GC?
            //      SEND PRUNE REQUESTS DOWNSTREAM

            return Task.FromResult(AssociatedMessage.Yield());
        }

    }
}
