using BlackSP.Core.Coordination;
using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using BlackSP.Kernel;
using Serilog;
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
        private readonly ILogger _logger;
        public CheckpointTakenHandler(WorkerGraphStateManager graphStateManager, ILogger logger)
        {
            _stateManager = graphStateManager ?? throw new ArgumentNullException(nameof(graphStateManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override Task<IEnumerable<ControlMessage>> Handle(CheckpointTakenPayload payload)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));

            //TODO: UPDATE LOCAL STATE WITH NEW CHECKPOINT AND ASSOCIATED SEQUENCE NRS
            //      DO RECOVERY LINE CALC
            //      GC?
            //      SEND PRUNE REQUESTS DOWNSTREAM
            _logger.Information("BIEP BOOP " + payload.OriginInstance + " TOOK CP: " + payload.CheckpointId);
            return Task.FromResult(AssociatedMessage.Yield());
        }

    }
}
