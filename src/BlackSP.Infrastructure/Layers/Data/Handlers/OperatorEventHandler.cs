using BlackSP.Core.Handlers;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    public class OperatorEventHandler : ForwardingPayloadHandlerBase<DataMessage, EventPayload>
    {

        private readonly IOperatorShell _operatorShell;

        public OperatorEventHandler(IOperatorShell operatorShell)
        {
            _operatorShell = operatorShell ?? throw new ArgumentNullException(nameof(operatorShell));
        }

        protected override Task<IEnumerable<DataMessage>> Handle(EventPayload payload)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));
            IEnumerable<DataMessage> result = _operatorShell.OperateOnEvent(payload.Event).Select(ev =>
            {
                var res = new DataMessage(AssociatedMessage.MetaData, ev.GetPartitionKey());
                res.AddPayload(new EventPayload { Event = ev });
                return res;
            }).ToList();

            return Task.FromResult(result);
        }
    }
}
