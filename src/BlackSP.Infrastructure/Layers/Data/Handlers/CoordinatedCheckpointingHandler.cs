using BlackSP.Checkpointing.Protocols;
using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    /// <summary>
    /// Implements chandy-lamports barrier algorithm for checkpoint decision making
    /// </summary>
    public class CoordinatedCheckpointingHandler : ForwardingPayloadHandlerBase<DataMessage, BarrierPayload>
    {

        private readonly ChandyLamportProtocol _protocol;
        private readonly IReceiverSource<DataMessage> _messageReceiver;
        private readonly ILogger _logger;

        public CoordinatedCheckpointingHandler(ChandyLamportProtocol.Factory protocolFactory,
            IReceiverSource<DataMessage> messageReceiver,
            ILogger logger)
        {
            _messageReceiver = messageReceiver; //optional argument
            _ = protocolFactory ?? throw new ArgumentNullException(nameof(protocolFactory));
            _protocol = protocolFactory.Invoke(_messageReceiver);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public CoordinatedCheckpointingHandler(ChandyLamportProtocol.Factory protocolFactory,
            ILogger logger)
        {
            _ = protocolFactory ?? throw new ArgumentNullException(nameof(protocolFactory));
            _protocol = protocolFactory.Invoke(_messageReceiver);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        protected override async Task<IEnumerable<DataMessage>> Handle(BarrierPayload payload)
        {
            var (endpoint, shardId) = _messageReceiver?.MessageOrigin ?? default;
            if (await _protocol.ReceiveBarrier(endpoint, shardId).ConfigureAwait(false))
            {
                AssociatedMessage.AddPayload(payload); //re-add payload if protocol indicates the value must be returned
            }
            return AssociatedMessage.Yield();
        }
    }
}
