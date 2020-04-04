using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Operators
{
    public interface IOperatorConfiguration
    {
    }

    public interface ISourceOperatorConfiguration<TEvent> : IOperatorConfiguration
        where TEvent : class, IEvent
    {
        string KafkaTopicName { get; }
    }

    public interface ISinkOperatorConfiguration<TEvent> : IOperatorConfiguration
        where TEvent : class, IEvent
    {
        string KafkaTopicName { get; }
    }

    public interface IFilterOperatorConfiguration<TEvent> : IOperatorConfiguration 
        where TEvent : class, IEvent
    {
        TEvent Filter(TEvent @event);
    }

    public interface IMapOperatorConfiguration<TIn, TOut> : IOperatorConfiguration 
        where TIn : class, IEvent
        where TOut : class, IEvent
    {
        IEnumerable<TOut> Map(TIn @event);
    }
}
