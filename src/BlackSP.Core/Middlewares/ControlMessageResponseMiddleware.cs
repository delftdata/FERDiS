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
            if(!message.TryGetPayload<WorkerRequestPayload>(out var payload))
            {
                //forward message
                return Task.FromResult(new List<ControlMessage>() { message }.AsEnumerable());

            }

            //consume message + forward results
            switch (payload.RequestType)
            {
                case RequestType.Status:
                    {
                        Console.WriteLine("STATUS REQUEST RECEIVED");
                        break;
                    }
                case RequestType.StartProcessing:
                {
                    break;
                }
                case RequestType.StopProcessing:
                {
                    break;
                }
                default: break;
            }


            return Task.FromResult(new List<ControlMessage>() { }.AsEnumerable());
        }
    }
}
