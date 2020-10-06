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

    static class Constants
    {
        public static int TotalEventsToSent = 1 * 10 * 1000000;
        public static int EventsBeforeProgressLog = 1 * 10000;
    }

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
            if (counter == Constants.TotalEventsToSent)
            {
                _logger.Debug($"Produced {Constants.TotalEventsToSent} events");
                counter = 0;
                Task.Delay(int.MaxValue, t).Wait(); //block forever
            }
            counter++;
            return new SampleEvent($"Key_{counter}", DateTime.Now, $"Value_{counter}");
        }
    }

    class SampleSinkOperator : ISinkOperator<SampleEvent>
    {
        private readonly ILogger _logger;

        [Checkpointable]
        private int totalEventCount = 0;

        public int EventCount;

        public double TotalLatencyMs;

        public DateTime StartTime;

        private bool isfirst = true;

        public SampleSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            EventCount = 0;
            TotalLatencyMs = 0;
        }
        public Task Sink(SampleEvent @event)
        {
            if (isfirst)
            {
                isfirst = false;
                StartTime = DateTime.Now;
                EventCount = 0;
                TotalLatencyMs = 0;
            }
            EventCount++;
            totalEventCount++;
            var latency = DateTime.Now - @event.EventTime;
            TotalLatencyMs += latency.TotalMilliseconds;
            
            if (EventCount % Constants.EventsBeforeProgressLog == 0)
            {
                //throughput
                //- avg (counter / total time)
                var runningTimeSeconds = (DateTime.Now - StartTime).TotalSeconds;
                var avgThroughputPerSec = EventCount / runningTimeSeconds;
                //
                //latency
                
                //- avg (total latency / counter)
                var avgLatencyMs = TotalLatencyMs / totalEventCount;
                //- min?
                //- max?
                _logger.Information($"Sink stats - time: {runningTimeSeconds:0.00}s - events: {totalEventCount} - throughput: {avgThroughputPerSec:0.00} e/s - latency: {avgLatencyMs:0}ms");
                StartTime = DateTime.Now;
                EventCount = 0;
                TotalLatencyMs = 0;
            }
            return Task.CompletedTask;
        }
    }

    class SampleMapOperator : IMapOperator<SampleEvent, SampleEvent>
    {
        public IEnumerable<SampleEvent> Map(SampleEvent @event)
        {
            yield return new SampleEvent(@event.Key, @event.EventTime, @event.Value);
        }
    }

    class SampleFilterOperator : IFilterOperator<SampleEvent>
    {
        public SampleEvent Filter(SampleEvent @event)
        {
            return @event;
        }
    }

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
            if(!window.Any())
            {
                var msg = "Aggragate was called with an empty window";
                _logger.Warning(msg);
                throw new Exception(msg);
            }
            yield return new SampleEvent2($"AggregateResult_{Counter++}", window.Max(x => x.EventTime), window.Count());
        }
    }
}

