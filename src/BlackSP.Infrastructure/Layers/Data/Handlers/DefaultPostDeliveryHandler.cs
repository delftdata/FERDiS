using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Serialization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{

    /// <summary>
    /// Hands of messages to the dispatcher's output queues<br/>
    /// Note this is a kind of hack to pull in the serialization step into the processing pipeline.. (probably requires redesign ?)
    /// </summary>
    public class DefaultPostDeliveryHandler : IHandler<DataMessage>
    {

        private readonly IObjectSerializer _serializer;
        private readonly IPartitioner<DataMessage> _partitioner;
        private readonly IDispatcher<DataMessage> _dispatcher;

        public DefaultPostDeliveryHandler(IObjectSerializer serializer,
            IPartitioner<DataMessage> partitioner,
            IDispatcher<DataMessage> dispatcher)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _partitioner = partitioner ?? throw new ArgumentNullException(nameof(partitioner));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public async Task<IEnumerable<DataMessage>> Handle(DataMessage message, CancellationToken t)
        {
            byte[] bytes = await _serializer.SerializeAsync(message, default).ConfigureAwait(true);
            foreach (var (config, shard) in _partitioner.Partition(message))
            {
                var outputQueue = _dispatcher.GetDispatchQueue(config, shard);
                await outputQueue.UnderlyingCollection.Writer.WriteAsync(bytes, t).ConfigureAwait(true);
            }
            return Enumerable.Empty<DataMessage>();
        }
    }
}
