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
    public class LogPruneResponseHandler : ForwardingPayloadHandlerBase<ControlMessage, LogPruneRequestPayload>
    {
        private readonly IMessageLoggingService<byte[]> _loggingService;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;

        public LogPruneResponseHandler(IMessageLoggingService<byte[]> loggingService,
                                    IVertexConfiguration vertexConfiguration,  
                                    ILogger logger)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        }

        protected override async Task<IEnumerable<ControlMessage>> Handle(LogPruneRequestPayload payload, CancellationToken t)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));

            //Verbose
            _logger.Verbose($"Handling prune request with arguments {payload.InstanceName}, {payload.SequenceNumber}");
            var pruneCount = _loggingService.Prune(payload.InstanceName, payload.SequenceNumber);
            var nextSeqNr = _loggingService.GetNextOutgoingSequenceNumber(payload.InstanceName);
            //Debug
            _logger.Information($"Pruned {pruneCount} messages from log to {payload.InstanceName}. Prune requested at seqNr: {payload.SequenceNumber}. Message log now at seqNr: {nextSeqNr}");

            //var response = new ControlMessage();
            AssociatedMessage.AddPayload(new WorkerResponsePayload() { });
            return AssociatedMessage.Yield();
        }

        
    }
}
