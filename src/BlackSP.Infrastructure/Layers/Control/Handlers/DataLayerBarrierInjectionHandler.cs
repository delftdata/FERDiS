using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Data;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace BlackSP.Infrastructure.Layers.Control.Handlers
{
    /// <summary>
    /// 
    /// </summary>
    public class DataLayerBarrierInjectionHandler : ForwardingPayloadHandlerBase<ControlMessage, BarrierPayload>
    {
        private readonly DataMessageProcessor _processor;
        private readonly ILogger _logger;

        public DataLayerBarrierInjectionHandler(DataMessageProcessor processor,
                                    ILogger logger)
        {            
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override Task<IEnumerable<ControlMessage>> Handle(BarrierPayload payload)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));

            var dataMsg = new DataMessage();
            dataMsg.AddPayload(payload);
            _processor.Inject(dataMsg);
            _logger.Information("Inserted barrier in data layer");
            return Task.FromResult(AssociatedMessage.Yield());
        }

    }
}
