using BlackSP.Checkpointing;
using BlackSP.Kernel.Operators;
using BlackSP.Sandbox.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Sandbox.Operators
{
    class SampleAggregateOperator : IAggregateOperator<SampleEvent, SampleEvent2>
    {
        private readonly ILogger _logger;

        public TimeSpan WindowSize => TimeSpan.FromSeconds(2);

        [Checkpointable]
        public int Counter;

        public SampleAggregateOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Counter = 0;
        }

        public IEnumerable<SampleEvent2> Aggregate(IEnumerable<SampleEvent> window)
        {
            if (!window.Any())
            {
                var msg = "Aggragate was called with an empty window";
                _logger.Warning(msg);
                throw new Exception(msg);
            }
            yield return new SampleEvent2($"AggregateResult_{Counter++}", window.Max(x => x.EventTime), window.Count());
        }
    }
}
