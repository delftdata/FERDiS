using BlackSP.Core.Coordination;
using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Handlers
{
    /// <summary>
    /// ControlMessage handler that handles responses of worker requests on the coordinator side
    /// </summary>
    public class WorkerResponseHandler : ForwardingPayloadHandlerBase<ControlMessage, WorkerResponsePayload>
    {

        private readonly WorkerGraphStateManager _stateManager;
        public WorkerResponseHandler(WorkerGraphStateManager graphStateManager)
        {
            _stateManager = graphStateManager ?? throw new ArgumentNullException(nameof(graphStateManager));
        }

        protected override Task<IEnumerable<ControlMessage>> Handle(WorkerResponsePayload payload, CancellationToken t)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));
            switch (payload.OriginalRequestType)
            {
                case WorkerRequestType.Status:
                    //status request currently empty..
                    break;
                case WorkerRequestType.StartProcessing:
                    //worker confirmed starting..
                    break;
                case WorkerRequestType.StopProcessing:
                    //worker confirmed stopping..
                    _stateManager.GetWorkerStateManager(payload.OriginInstanceName).FireTrigger(WorkerStateTrigger.DataProcessorHaltCompleted);
                    break;
                default:
                    throw new InvalidOperationException($"Received response to worker request of type {payload.OriginalRequestType}, which is not implemented in type {this.GetType()}");
            }
            //this handler consumes the message if it has the workerstatus payload
            return Task.FromResult(Enumerable.Empty<ControlMessage>());
        }

    }
}
