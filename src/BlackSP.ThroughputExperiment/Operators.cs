using BlackSP.OperatorShells;
using BlackSP.Kernel.Operators;
using BlackSP.ThroughputExperiment.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using BlackSP.Checkpointing.Attributes;

namespace BlackSP.ThroughputExperiment
{

    class SampleSourceOperator : ISourceOperator<SampleEvent>
    {
        private readonly ILogger _logger;
        
        [Checkpointable]
        private int counter = 0;

        public SampleSourceOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public SampleEvent ProduceNext(CancellationToken t)
        {
            counter++;
            return new SampleEvent($"Key_{counter}", DateTime.Now, $"Value_{counter}");
        }
    }

    class SampleSinkOperator : ISinkOperator<SampleEvent>
    {
        private readonly ILogger _logger;

        [Checkpointable]
        private int totalEventCount = 0;

        public SampleSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            totalEventCount = 0;
        }
        public Task Sink(SampleEvent @event)
        {
            totalEventCount++;
            return Task.CompletedTask;
        }
    }
}

