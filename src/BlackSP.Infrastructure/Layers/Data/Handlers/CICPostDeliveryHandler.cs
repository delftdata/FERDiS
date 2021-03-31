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

        private readonly ILogger _logger;

        public CICPostDeliveryHandler(
            HMNRProtocol hmnrProtocol,
            
            ILogger logger)
        {
            _hmnrProtocol = hmnrProtocol ?? throw new ArgumentNullException(nameof(hmnrProtocol));

            
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<DataMessage>> Handle(DataMessage message)
        {
            var (clock, ckpt, taken) = _hmnrProtocol.GetPiggybackData();
            message.AddPayload(new CICPayload { clock = clock, ckpt = ckpt, taken = taken });
            return message.Yield();
        }

    }
}
