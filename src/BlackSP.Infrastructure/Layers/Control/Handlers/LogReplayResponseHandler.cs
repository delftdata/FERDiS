using BlackSP.Core.Coordination;
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
        private readonly IMessageLoggingService<DataMessage> _logService;
        private readonly IDispatcher<DataMessage> _dataDispatcher;
        private readonly ILogger _logger;

        public LogReplayResponseHandler(DataMessageProcessor dataProcessor,
            IMessageLoggingService<DataMessage> loggingService,
            IDispatcher<DataMessage> dispatcher,  
            ILogger logger)
        {
            _dataProcessor = dataProcessor ?? throw new ArgumentNullException(nameof(dataProcessor));
            _logService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _dataDispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        }

        protected override async Task<IEnumerable<ControlMessage>> Handle(LogReplayRequestPayload payload)
        {
            //Verbose
            _logger.Fatal($"Handling log replay request");
            await _dataProcessor.Pause().ConfigureAwait(false);
            try
            {
                foreach (var entry in payload.ReplayMap)
                {
                    //Debug
                    _logger.Fatal($"Replaying log to {entry.Key} from sequencenr {entry.Value}");
                    foreach (var (seqnr, msg) in _logService.Replay(entry.Key, entry.Value))
                    {
                        await _dataDispatcher.Dispatch(msg, default).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _dataProcessor.Unpause();
            }
            _logger.Fatal($"Handled log replay request");

            return AssociatedMessage.Yield();
        }

    }
}
