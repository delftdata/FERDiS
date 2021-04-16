using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using BlackSP.Infrastructure.Layers.Data;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Handlers
{
    /// <summary>
    /// ControlMessage handler dedicated to handling requests on the worker side
    /// </summary>
    public class LogPruneRequestHandler : ForwardingPayloadHandlerBase<ControlMessage, LogPruneRequestPayload>
    {
        private readonly IMessageLoggingService<DataMessage> _loggingService;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;

        public LogPruneRequestHandler(IMessageLoggingService<DataMessage> loggingService,
                                    IVertexConfiguration vertexConfiguration,  
                                    ILogger logger)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        }

        protected override async Task<IEnumerable<ControlMessage>> Handle(LogPruneRequestPayload payload)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));

            //Verbose
            _logger.Fatal($"Handling prune request with arguments {payload.InstanceName}, {payload.SequenceNumber}");
            var pruneCount = _loggingService.Prune(payload.InstanceName, payload.SequenceNumber);
            //Debug
            _logger.Fatal($"Pruned {pruneCount} messages from log to {payload.InstanceName}. Log now at seqNr: {payload.SequenceNumber}");

            var response = new ControlMessage();
            response.AddPayload(new WorkerResponsePayload()
            {
            });
            return response.Yield();
        }

        
    }
}
