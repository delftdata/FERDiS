using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Data;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace BlackSP.Infrastructure.Layers.Control.Handlers
{
    /// <summary>
    /// Injects a barrier payload into the data layer
    /// </summary>
    public class ChandyLamportBarrierInjectionHandler : ForwardingPayloadHandlerBase<ControlMessage, BarrierPayload>
    {
        private readonly DataMessageProcessor _processor;
        private readonly ILogger _logger;

        public ChandyLamportBarrierInjectionHandler(DataMessageProcessor processor,
                                    ILogger logger)
        {            
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override Task<IEnumerable<ControlMessage>> Handle(BarrierPayload payload, CancellationToken t)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));

            var dataMsg = new DataMessage();
            dataMsg.AddPayload(payload);
            try
            {
                _processor.Inject(dataMsg);
                _logger.Information("Inserted barrier in data layer");
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error inserting barrier");
                throw;
            }
            return Task.FromResult(AssociatedMessage.Yield());
        }

    }
}
