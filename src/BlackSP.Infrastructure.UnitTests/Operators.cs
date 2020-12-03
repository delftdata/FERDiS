using BlackSP.Infrastructure.UnitTests.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.UnitTests
{
    class SampleSourceOperator : ISourceOperator<EventA>
    {
        public string KafkaTopicName => "";

        public EventA ProduceNext(CancellationToken t)
        {
            return new EventA();
        }
    }

    class SampleFilterOperator : IFilterOperator<EventA>
    {
        public EventA Filter(EventA @event)
        {
            throw new System.NotImplementedException();
        }
    }

    class SampleMapOperator : IMapOperator<EventA, EventB>
    {
        //public SampleMapOperator(string x)
        //{

        //}
        public IEnumerable<EventB> Map(EventA @event)
        {
            throw new System.NotImplementedException();
        }
    }

    class SampleJoinOperator : IJoinOperator<EventA, EventB, EventC>
    {
        public TimeSpan WindowSize => TimeSpan.FromMinutes(5);
        public TimeSpan WindowSlideSize => TimeSpan.FromSeconds(5);


        public EventC Join(EventA matchA, EventB matchB)
        {
            throw new NotImplementedException();
        }

        public bool Match(EventA testA, EventB testB)
        {
            throw new NotImplementedException();
        }
    }

    class SampleAggregateOperator : IAggregateOperator<EventC, EventD>
    {
        public TimeSpan WindowSize => TimeSpan.FromMinutes(5);
        public TimeSpan WindowSlideSize => TimeSpan.FromSeconds(5);

        public IEnumerable<EventD> Aggregate(IEnumerable<EventC> window)
        {
            throw new NotImplementedException();
        }
    }

    class SampleSinkOperator : ISinkOperator<EventD>
    {
        public string KafkaTopicName => "";

        public Task Sink(EventD @event)
        {
            throw new NotImplementedException();
        }
    }

}

