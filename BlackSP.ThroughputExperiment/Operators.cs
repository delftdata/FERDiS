using BlackSP.Core.OperatorSockets;
using BlackSP.Kernel.Operators;
using BlackSP.ThroughputExperiment.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.ThroughputExperiment
{
    class SampleSourceOperator : ISourceOperator<SampleEvent>
    {
        public string KafkaTopicName => throw new NotImplementedException();

        private int counter = 0;

        public SampleSourceOperator()
        {
            //LocalStateExample_Counter = 0;
        }

        public IEnumerable<SampleEvent> GetTestEvents()
        {
            var events = new List<SampleEvent>();
            
            for(int i = 0; i < 15000; i++)
            {
                if(counter > 10 * 1000 * 1000) { break; }
                events.Add(new SampleEvent($"Key_{counter}", DateTime.Now, $"Key_{counter}"));
                counter++;
            }

            if(counter % 100000 == 0)
            {
                Console.WriteLine($">> Source emitting 100.000 events");
            }
            return events.AsEnumerable();
        }
    }

    class SampleSinkOperator : ISinkOperator<SampleEvent>
    {
        public string KafkaTopicName => throw new NotImplementedException();

        public int EventCount { get; set; }
        public double TotalLatencyMs { get; set; }
        public DateTime StartTime { get; set; }

        public SampleSinkOperator()
        {
            EventCount = 0;
            TotalLatencyMs = 0;
        }
        private bool isfirst = true;
        public Task Sink(SampleEvent @event)
        {
            if (isfirst)
            {
                isfirst = false;
            }
            EventCount++;
            var latency = DateTime.Now - @event.EventTime;
            TotalLatencyMs += latency.TotalMilliseconds;
            if (EventCount % 100000 == 0)
            {

                //throughput
                //- avg (counter / total time)
                var runningTimeSeconds = (DateTime.Now - StartTime).TotalSeconds;
                var avgThroughputPerSec = EventCount / runningTimeSeconds;
                //
                //latency
                
                //- avg (total latency / counter)
                var avgLatencyMs = TotalLatencyMs / EventCount;
                //- min
                //- max
                Console.WriteLine($">> Sink stats - time: {runningTimeSeconds:0.00}s - events: {EventCount} - throughput: {avgThroughputPerSec:0.00} e/s - latency: {avgLatencyMs:0}ms");
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

            if(counter%100000 == 0)
            {
                Console.WriteLine($">> Mapper is at {counter}");

            }
            yield return new SampleEvent(@event.Key, @event.EventTime, @event.Value);
        }
    }

    class SampleFilterOperator : IFilterOperator<SampleEvent>
    {
        public SampleEvent Filter(SampleEvent @event)
        {
            //Console.WriteLine($">> filter got {@event.Key}");
            return @event;
        }
    }

    class SampleAggregateOperator : IAggregateOperator<SampleEvent, SampleEvent2>
    {
        public TimeSpan WindowSize => TimeSpan.FromSeconds(1);
        public int Counter { get; set; }

        public SampleAggregateOperator()
        {
            Counter = 0;
        }

        public IEnumerable<SampleEvent2> Aggregate(IEnumerable<SampleEvent> window)
        {
            if(!window.Any())
            {
                throw new Exception("Dude?");
            }
            yield return new SampleEvent2($"AggregateResult_{Counter++}", DateTime.Now, $"{window.Count()} Events");
        }
    }
}

