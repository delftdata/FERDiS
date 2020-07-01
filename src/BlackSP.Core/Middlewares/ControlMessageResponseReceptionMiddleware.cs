using BlackSP.Core;
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
    public class ControlMessageResponseReceptionMiddleware : IMiddleware<ControlMessage>
    {

        private readonly WorkerStateMonitor _workerStateMonitor;

        public ControlMessageResponseReceptionMiddleware(WorkerStateMonitor workerStateMonitor)
        {
            _workerStateMonitor = workerStateMonitor ?? throw new ArgumentNullException(nameof(workerStateMonitor));
        }

        public Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            if(!message.TryGetPayload<WorkerStatusPayload>(out var payload))
            {
                //forward message
                return Task.FromResult(new List<ControlMessage>() { message }.AsEnumerable());
            }
            //received message with worker status payload

            string origin = payload.OriginInstanceName; //origin
            Console.WriteLine($"{origin} reported state");
            _workerStateMonitor.UpdateStateFromReport(origin, payload);

            //this middleware always consumes the message if it has the workerstatus payload
            return Task.FromResult(new List<ControlMessage>() { }.AsEnumerable());
        }
    }
}
