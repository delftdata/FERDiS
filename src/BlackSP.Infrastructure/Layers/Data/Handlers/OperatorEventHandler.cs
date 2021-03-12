using BlackSP.Core.Handlers;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    /// <summary>
    /// Hands incoming events to the locally registered operator, only forwards resulting events as new messages
    /// </summary>
    public class OperatorEventHandler : ForwardingPayloadHandlerBase<DataMessage, EventPayload>
    {

        private readonly IOperatorShell _operatorShell;
        private readonly ISource<DataMessage> _messageSource;

        public OperatorEventHandler(IOperatorShell operatorShell, ISource<DataMessage> messageSource)
        {
            _operatorShell = operatorShell ?? throw new ArgumentNullException(nameof(operatorShell));
            _messageSource = messageSource ?? throw new ArgumentNullException(nameof(messageSource));
        }

        protected override async Task<IEnumerable<DataMessage>> Handle(EventPayload payload)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));

            var (endpointConfig, shardId) = _messageSource.MessageOrigin;

            var events = await _operatorShell.OperateOnEvent(payload.Event, endpointConfig.IsBackchannel).ConfigureAwait(false);
            IEnumerable<DataMessage> result = events.Select(ev =>
            {
                var res = new DataMessage(AssociatedMessage.CreatedAtUtc, AssociatedMessage.MetaData, ev.Key != null ? ev.GetPartitionKey() : (int?)null);
                res.AddPayload(new EventPayload { Event = ev });
                return res;
            });
            return result;
        }
    }
}
