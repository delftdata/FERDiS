using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    public class MessageLoggingPostDeliveryHandler : IHandler<DataMessage>
    {

        private readonly IMessageLoggingService<DataMessage> _loggingService;
        private readonly IPartitioner<DataMessage> _partitioner;

        public MessageLoggingPostDeliveryHandler(
            IMessageLoggingService<DataMessage> loggingService,
            IPartitioner<DataMessage> partitioner)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _partitioner = partitioner ?? throw new ArgumentNullException(nameof(partitioner));
        }


        public Task<IEnumerable<DataMessage>> Handle(DataMessage message)
        {
            return Task.FromResult(HandleSync(message));
        }

        public IEnumerable<DataMessage> HandleSync(DataMessage message)
        {
            var targets = _partitioner.Partition(message);

            foreach (var (endpoint, shard) in targets)
            {
                var targetInstance = endpoint.GetRemoteInstanceName(shard);
                var seqNr = _loggingService.Append(targetInstance, message);
                var payload = new SequenceNumberPayload { SequenceNumber = seqNr };

                var msgCopy = new DataMessage(message);
                msgCopy.TargetOverride = (endpoint, shard);
                msgCopy.AddPayload(payload);

                yield return msgCopy;
            }
        }
    }
}
