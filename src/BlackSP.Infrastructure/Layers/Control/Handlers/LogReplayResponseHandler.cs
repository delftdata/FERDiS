using BlackSP.Core.Coordination;
using BlackSP.Core.Extensions;
using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using BlackSP.Infrastructure.Layers.Data;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Handlers
{
    /// <summary>
    /// Handles log replay payloads to replay message logs
    /// </summary>
    public class LogReplayResponseHandler : ForwardingPayloadHandlerBase<ControlMessage, LogReplayRequestPayload>
    {
        private readonly DataMessageProcessor _dataProcessor;
        private readonly IMessageLoggingService<byte[]> _logService;
        private readonly IDispatcher<DataMessage> _dataDispatcher;
        private readonly ILogger _logger;
        private readonly IVertexConfiguration _vertexConfiguration;

        public LogReplayResponseHandler(DataMessageProcessor dataProcessor,
            IMessageLoggingService<byte[]> loggingService,
            IDispatcher<DataMessage> dispatcher,  
            ILogger logger,
            IVertexConfiguration vertexConfiguration)
        {
            _dataProcessor = dataProcessor ?? throw new ArgumentNullException(nameof(dataProcessor));
            _logService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _dataDispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));

        }

        protected override async Task<IEnumerable<ControlMessage>> Handle(LogReplayRequestPayload payload, CancellationToken t)
        {
            _logger.Debug($"Handling log replay request - pausing processor");
            await _dataProcessor.Pause().ConfigureAwait(false);
            try
            {
                foreach (var entry in payload.ReplayMap)
                {
                    var instanceName = entry.Key;
                    var sequenceNr = entry.Value;
                    _logger.Debug($"Replaying log to {entry.Key} from sequencenr {entry.Value}");
                    var (targetConf, shard) = _vertexConfiguration.GetTargetPairByInstanceName(instanceName);
                    var dispatchQueue = _dataDispatcher.GetDispatchQueue(targetConf, shard);
                    var i = 0;
                    foreach (var (seqnr, msg) in _logService.Replay(instanceName, sequenceNr))
                    {
                        await dispatchQueue.UnderlyingCollection.Writer.WriteAsync(msg, default).ConfigureAwait(false);
                        i++;
                    }
                    _logger.Information($"Replayed message log to {entry.Key} from sequencenr {entry.Value} till {entry.Value + i} ({i} messages)");
                }
            }
            finally
            {
                _dataProcessor.Unpause();
            }
            _logger.Debug($"Handled log replay request sucessfully - unpausing processor");

            return AssociatedMessage.Yield();
        }

    }
}
