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

namespace BlackSP.ThroughputExperiment
{

    static class Constants
    {
        public static int TotalEventsToSent = 1 * 10 * 1000000;
        public static int EventsBeforeProgressLog = 1 * 100000;
    }

    class SampleSourceOperator : ISourceOperator<SampleEvent>
    {
        private readonly ILogger _logger;
        private int counter = 0;

        public SampleSourceOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public SampleEvent ProduceNext(CancellationToken t)
        {
            //Task.Delay(10, t).Wait();
            if (counter == Constants.TotalEventsToSent)
            {
                _logger.Debug($"Produced {Constants.TotalEventsToSent} events");
                counter = 0;
            }
            counter++;
            return new SampleEvent($"Key_{counter}", DateTime.Now, $"Value_{counter}");
        }
    }

    class SampleSinkOperator : ISinkOperator<SampleEvent>
    {
        private readonly ILogger _logger;

        private int totalEventCount = 0;
        public int EventCount { get; set; }
        public double TotalLatencyMs { get; set; }
        public DateTime StartTime { get; set; }

        public SampleSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            EventCount = 0;
            TotalLatencyMs = 0;
        }
        private bool isfirst = true;
        public Task Sink(SampleEvent @event)
        {
            if (isfirst)
            {
                isfirst = false;
                StartTime = DateTime.Now;
                EventCount = 0;
                TotalLatencyMs = 0;
            }
            totalEventCount++;
            EventCount++;
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
                var avgLatencyMs = TotalLatencyMs / EventCount;
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
        private int counter = 0;

        public IEnumerable<SampleEvent> Map(SampleEvent @event)
        {
            counter++;
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
        public int Counter { get; set; }

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
            yield return new SampleEvent2($"AggregateResult_{Counter++}", window.Max(x => x.EventTime), $"{window.Count()} Events");
        }
    }
}

