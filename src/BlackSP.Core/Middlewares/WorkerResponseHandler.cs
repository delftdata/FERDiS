using BlackSP.Core;
using BlackSP.Core.Coordination;
using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Core.Monitors;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Core.Middlewares
{
    /// <summary>
    /// ControlMessage handler that handles responses of worker requests on the coordinator side
    /// </summary>
    public class WorkerResponseHandler : IMiddleware<ControlMessage>
    {

        private readonly WorkerGraphStateManager _stateManager;
        public WorkerResponseHandler(WorkerGraphStateManager graphStateManager)
        {
            _stateManager = graphStateManager ?? throw new ArgumentNullException(nameof(graphStateManager));
        }

        public Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            if(!message.TryGetPayload<WorkerResponsePayload>(out var payload))
            {
                //forward message
                return Task.FromResult(message.Yield());
            }
            //received message with worker status payload

            string origin = payload.OriginInstanceName; //origin
            WorkerStateTrigger trigger;
            switch (payload.OriginalRequestType)
            {
                case WorkerRequestType.Status:
                    //TODO: consider what to do if the worker lost connection to some neighbours
                    trigger = payload.UpstreamFullyConnected && payload.DownstreamFullyConnected ? WorkerStateTrigger.NetworkConnected : WorkerStateTrigger.NetworkDisconnected;
                    break;
                case WorkerRequestType.StartProcessing:
                case WorkerRequestType.StopProcessing:
                    trigger = payload.DataProcessActive ? WorkerStateTrigger.DataProcessorStart : WorkerStateTrigger.DataProcessorHalt;
                    break;
                default:
                    throw new InvalidOperationException($"Received response to worker request of type {payload.OriginalRequestType}, which is not implemented in type {this.GetType()}");
            }
            
            //TODO: should this even be here?
            //var workerStateManager = _stateManager.GetWorkerStateManager(origin);
            //workerStateManager.FireTrigger(trigger);

            //this middleware always consumes the message if it has the workerstatus payload
            return Task.FromResult(Enumerable.Empty<ControlMessage>());
        }
    }
}
