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
    public class OperatorMiddleware : IMiddleware<DataMessage>
    {

        private readonly IOperatorShell _operatorShell;

        public OperatorMiddleware(IOperatorShell operatorShell)
        {
            _operatorShell = operatorShell ?? throw new ArgumentNullException(nameof(operatorShell));
        }

        public Task<IEnumerable<DataMessage>> Handle(DataMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            
            if(!message.TryGetPayload<EventPayload>(out var eventPayload))
            {
                return Task.FromResult(Enumerable.Repeat(message, 1));
            }
            
            IEvent payload = eventPayload.Event;
            IEnumerable<DataMessage> result = _operatorShell.OperateOnEvent(payload).Select(ev =>
                {
                    var res = new DataMessage(message.MetaData);
                    res.AddPayload(new EventPayload { Event = ev });
                    return res;
                }).ToList();

            return Task.FromResult(result);
        }
    }
}
