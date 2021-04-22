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
            var targets = _partitioner.Partition(message);

            var seqNrs = new List<int>();
            foreach (var (endpoint, shard) in targets)
            {
                seqNrs.Add(_loggingService.Append(endpoint.RemoteInstanceNames.ElementAt(shard), message));
            }
            var seqNr = seqNrs.First();
            if(!seqNrs.All(s => s == seqNr)) //TODO: mixing broadcasts with partition is not the issue with NHOP --> its two endpoints which both get partitioning!
            {
                throw new NotSupportedException("Current implementation does not support multiple output endpoints?");
            }
            var payload = new SequenceNumberPayload { SequenceNumber = seqNr };
            message.AddPayload(payload);
            return Task.FromResult(message.Yield());
        }
    }
}
