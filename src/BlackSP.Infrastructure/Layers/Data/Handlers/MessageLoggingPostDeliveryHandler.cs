using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    public class MessageLoggingPostDeliveryHandler : IHandler<DataMessage>
    {

        private readonly IMessageLoggingService<byte[]> _loggingService;
        private readonly IPartitioner<DataMessage> _partitioner;
        private readonly IObjectSerializer _serializer;
        private readonly IDispatcher<DataMessage> _dispatcher;

        public MessageLoggingPostDeliveryHandler(
            IMessageLoggingService<byte[]> loggingService,
            IPartitioner<DataMessage> partitioner,
            IObjectSerializer serializer,
            IDispatcher<DataMessage> dispatcher)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _partitioner = partitioner ?? throw new ArgumentNullException(nameof(partitioner));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }


        public async Task<IEnumerable<DataMessage>> Handle(DataMessage message, CancellationToken t)
        {
            var targets = _partitioner.Partition(message);

            if (targets.Count() == 1)
            {
                var (endpoint, shard) = targets.First();
                var targetInstance = endpoint.GetRemoteInstanceName(shard);
                var seqNr = _loggingService.GetNextOutgoingSequenceNumber(targetInstance);
                var payload = new SequenceNumberPayload { SequenceNumber = seqNr };
                message.AddPayload(payload);

                byte[] bytes = await _serializer.SerializeAsync(message, t).ConfigureAwait(true);
                _loggingService.Append(targetInstance, bytes);

                var outputQueue = _dispatcher.GetDispatchQueue(endpoint, shard);
                await outputQueue.UnderlyingCollection.Writer.WriteAsync(bytes, t).ConfigureAwait(true);
                return Enumerable.Empty<DataMessage>();
            }

            foreach (var (endpoint, shard) in targets)
            {
                var targetInstance = endpoint.GetRemoteInstanceName(shard);

                var msgCopy = new DataMessage(message);
                msgCopy.TargetOverride = (endpoint, shard);
                var seqNr = _loggingService.GetNextOutgoingSequenceNumber(targetInstance);
                var payload = new SequenceNumberPayload { SequenceNumber = seqNr };
                msgCopy.AddPayload(payload);

                byte[] bytes = await _serializer.SerializeAsync(message, t).ConfigureAwait(true);
                _loggingService.Append(targetInstance, bytes);
                var outputQueue = _dispatcher.GetDispatchQueue(endpoint, shard);
                await outputQueue.UnderlyingCollection.Writer.WriteAsync(bytes, t).ConfigureAwait(true);
            }

            return Enumerable.Empty<DataMessage>();
        }
    }
}
