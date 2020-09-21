using BlackSP.Core.Coordination;
using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Core.Monitors;
using BlackSP.Kernel.MessageProcessing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Checkpointing
{
    public class CheckpointRestoreResponseHandler : IHandler<ControlMessage>
    {
        private readonly WorkerGraphStateManager _graphManager;
        private readonly ILogger _logger;
        public CheckpointRestoreResponseHandler(WorkerGraphStateManager graphManager, ILogger logger)
        {
            _graphManager = graphManager ?? throw new ArgumentNullException(nameof(graphManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            if(message.TryGetPayload<CheckpointRestoreCompletionPayload>(out var payload))
            {
                _logger.Information($"Received checkpoint restore completion response from {payload.InstanceName} with checkpointId: {payload.CheckpointId}");
                var workerManager = _graphManager.GetWorkerStateManager(payload.InstanceName);
                workerManager.FireTrigger(WorkerStateTrigger.CheckpointRestoreCompleted, payload.CheckpointId);
                return Task.FromResult(Enumerable.Empty<ControlMessage>());
            }
            return Task.FromResult(message.Yield());

        }
    }
}
