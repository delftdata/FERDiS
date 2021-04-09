using BlackSP.Core.Coordination;
using BlackSP.Core.Extensions;
using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Core.Models;
using BlackSP.Core.Observers;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using BlackSP.Kernel.MessageProcessing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Handlers
{
    public class CheckpointRestoreResponseHandler : ForwardingPayloadHandlerBase<ControlMessage, CheckpointRestoreCompletionPayload>
    {
        private readonly WorkerGraphStateManager _graphManager;
        private readonly ILogger _logger;
        public CheckpointRestoreResponseHandler(WorkerGraphStateManager graphManager, ILogger logger)
        {
            _graphManager = graphManager ?? throw new ArgumentNullException(nameof(graphManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override Task<IEnumerable<ControlMessage>> Handle(CheckpointRestoreCompletionPayload payload)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));
            _logger.Debug($"Received checkpoint restore completion response from {payload.InstanceName} with checkpointId: {payload.CheckpointId}");
            var workerManager = _graphManager.GetWorkerStateManager(payload.InstanceName);
            workerManager.FireTrigger(WorkerStateTrigger.CheckpointRestoreCompleted, payload.CheckpointId);
            return Task.FromResult(Enumerable.Empty<ControlMessage>());
        }
    }
}
