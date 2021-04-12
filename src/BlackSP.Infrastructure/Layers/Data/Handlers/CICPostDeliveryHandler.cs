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
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    public class CICPostDeliveryHandler : IHandler<DataMessage>
    {

        private readonly HMNRProtocol _hmnrProtocol;
        private readonly IPartitioner<DataMessage> _partitioner;
        private readonly ILogger _logger;

        public CICPostDeliveryHandler(
            HMNRProtocol hmnrProtocol,
            IPartitioner<DataMessage> partitioner,
            ILogger logger)
        {
            _hmnrProtocol = hmnrProtocol ?? throw new ArgumentNullException(nameof(hmnrProtocol));
            _partitioner = partitioner ?? throw new ArgumentNullException(nameof(partitioner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<DataMessage>> Handle(DataMessage message)
        {
            foreach(var (endpoint, shard) in _partitioner.Partition(message))
            {
                _hmnrProtocol.BeforeSend(endpoint.GetRemoteInstanceName(shard));
            }
            
            var (clock, ckpt, taken) = _hmnrProtocol.GetPiggybackData();
            message.AddPayload(new CICPayload { clock = clock, ckpt = ckpt, taken = taken });
            return message.Yield();
        }

    }
}
