using BlackSP.Checkpointing.Protocols;
using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Control;
using BlackSP.Infrastructure.Layers.Control.Payloads;
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
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    /// <summary>
    /// Implements chandy-lamports barrier algorithm for checkpoint decision making
    /// </summary>
    public class ChandyLamportBarrierHandler : ForwardingPayloadHandlerBase<DataMessage, BarrierPayload>
    {

        private readonly ChandyLamportProtocol _protocol;
        private readonly IReceiverSource<DataMessage> _messageReceiver;
        private readonly IDispatcher<ControlMessage> _controlDispatcher;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;

        public ChandyLamportBarrierHandler(ChandyLamportProtocol.Factory protocolFactory,
            IReceiverSource<DataMessage> messageReceiver,
            IDispatcher<ControlMessage> controlDispatcher,
            IVertexConfiguration vertexConfiguration,
            ILogger logger)
        {
            _messageReceiver = messageReceiver; //optional argument
            _ = protocolFactory ?? throw new ArgumentNullException(nameof(protocolFactory));
            _protocol = protocolFactory.Invoke(_messageReceiver);
            _controlDispatcher = controlDispatcher ?? throw new ArgumentNullException(nameof(controlDispatcher));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ChandyLamportBarrierHandler(ChandyLamportProtocol.Factory protocolFactory,
            IDispatcher<ControlMessage> controlDispatcher,
            IVertexConfiguration vertexConfiguration,
            ILogger logger)
        {
            _ = protocolFactory ?? throw new ArgumentNullException(nameof(protocolFactory));
            _protocol = protocolFactory.Invoke(_messageReceiver);
            _controlDispatcher = controlDispatcher ?? throw new ArgumentNullException(nameof(controlDispatcher));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        protected override async Task<IEnumerable<DataMessage>> Handle(BarrierPayload payload, CancellationToken t)
        {
            var (endpoint, shardId) = _messageReceiver?.MessageOrigin ?? default;
            _logger.Information($"Handling barrier from upstream instance: {endpoint?.GetRemoteInstanceName(shardId)}");
            if (await _protocol.ReceiveBarrier(endpoint, shardId).ConfigureAwait(false))
            {
                AssociatedMessage.AddPayload(payload); //re-add payload if protocol indicates the value must be returned
                //var msg = new ControlMessage();
                //msg.AddPayload(new CheckpointTakenPayload { OriginInstance = _vertexConfiguration.InstanceName });
                ///*await */_controlDispatcher.Dispatch(msg, t); //explicitly do not wait for this task to avoid slowing the rate of processing
            }
            _logger.Information($"Handled barrier from upstream instance: {endpoint?.GetRemoteInstanceName(shardId)}");
            return AssociatedMessage.Yield();
        }
    }
}
