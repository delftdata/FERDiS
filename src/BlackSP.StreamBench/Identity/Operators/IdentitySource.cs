using BlackSP.Checkpointing;
using BlackSP.Kernel.Operators;
using BlackSP.StreamBench.Identity.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BlackSP.StreamBench.Identity.Operators
{
    class IdentitySource : ISourceOperator<IdentityEvent>
    {
        private readonly ILogger _logger;

        [Checkpointable]
        private int counter = 0;

        public IdentitySource(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IdentityEvent ProduceNext(CancellationToken t)
        {
            counter++;
            return new IdentityEvent($"Key_{counter}", DateTime.Now, $"Value_{counter}");
        }
    }
}
