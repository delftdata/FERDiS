using BlackSP.Core.Coordination;
using BlackSP.Core.Handlers;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
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

        protected override Task<IEnumerable<ControlMessage>> Handle(WorkerResponsePayload payload)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));
            switch (payload.OriginalRequestType)
            {
                case WorkerRequestType.Status:
                    //TODO: consider what to do if the worker lost connection to some neighbours
                    break;
                case WorkerRequestType.StartProcessing:
                    //started, nice
                    break;
                case WorkerRequestType.StopProcessing:
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
