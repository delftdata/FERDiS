using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.MessageProcessing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    public class MessageLoggingPreDeliveryHandler : ForwardingPayloadHandlerBase<DataMessage, SequenceNumberPayload>
    {

        private readonly IMessageLoggingService<DataMessage> _loggingService;
        private readonly ISource<DataMessage> _source;
        private readonly ILogger _logger;

        public MessageLoggingPreDeliveryHandler(
            IMessageLoggingService<DataMessage> loggingService,
            ISource<DataMessage> source,
            ILogger logger)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override Task<IEnumerable<DataMessage>> Handle(SequenceNumberPayload payload)
        {
            var (endpoint, shard) = _source.MessageOrigin;
            var origin = endpoint.GetRemoteInstanceName(shard);
            if(_loggingService.Receive(origin, payload.SequenceNumber))
            {
                _logger.Verbose($"Received message with sequence number {payload.SequenceNumber} from {origin}");
                return Task.FromResult(AssociatedMessage.Yield());
            } 
            
            _logger.Debug($"Dropping message with sequence number {payload.SequenceNumber} (expected {_loggingService.ReceivedSequenceNumbers[origin]+1}) from {origin}");
            return Task.FromResult(Enumerable.Empty<DataMessage>());
        }
    }
}
