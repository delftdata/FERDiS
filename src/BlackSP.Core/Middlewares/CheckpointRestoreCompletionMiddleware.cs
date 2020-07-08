using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Core.Monitors;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.Middlewares
{
    public class CheckpointRestoreCompletionMiddleware : IMiddleware<ControlMessage>
    {

        private readonly WorkerStateMonitor _workerStateMonitor;
        public CheckpointRestoreCompletionMiddleware(WorkerStateMonitor workerStateMonitor)
        {
            _workerStateMonitor = workerStateMonitor ?? throw new ArgumentNullException(nameof(workerStateMonitor));
        }

        public Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            if(message.TryGetPayload<CheckpointRestoreCompletionPayload>(out var payload))
            {
                _workerStateMonitor.NotifyRestoreCompletion(payload.InstanceName);
                return Task.FromResult(Enumerable.Empty<ControlMessage>());

            }
            return Task.FromResult(Enumerable.Repeat(message, 1));

        }
    }
}
