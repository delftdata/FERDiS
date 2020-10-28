using BlackSP.Checkpointing;
using BlackSP.Kernel.Operators;
using BlackSP.StreamBench.Identity.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.StreamBench.Identity.Operators
{
    class IdentitySink : ISinkOperator<IdentityEvent>
    {
        private readonly ILogger _logger;

        [Checkpointable]
        private int totalEventCount = 0;

        public IdentitySink(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            totalEventCount = 0;
        }
        public Task Sink(IdentityEvent @event)
        {
            totalEventCount++;
            return Task.CompletedTask;
        }
    }
}
