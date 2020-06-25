using BlackSP.Core;
using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Core.Middlewares
{
    public class ControlMessageResponseMiddleware : IMiddleware<ControlMessage>
    {


        public ControlMessageResponseMiddleware()
        {
        }

        public Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            if(!message.TryGetPayload<WorkerRequestPayload>(out var payload) || payload.RequestType != WorkerRequestType.Status)
            {
                //forward message
                return Task.FromResult(new List<ControlMessage>() { message }.AsEnumerable());
            }

            Console.WriteLine("STATUS REQUEST RECEIVED");

            //message consumed, no results to advance
            return Task.FromResult(new List<ControlMessage>() { }.AsEnumerable());
        }
    }
}
