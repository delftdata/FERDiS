using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Operators
{
    public interface IOperator
    {
    }

    public interface ISourceOperator<TEvent> : IOperator
        where TEvent : class, IEvent
    {
        string KafkaTopicName { get; }
    }

    public interface ISinkOperator<TEvent> : IOperator
        where TEvent : class, IEvent
    {
        string KafkaTopicName { get; }
    }

    public interface IFilterOperator<TEvent> : IOperator 
        where TEvent : class, IEvent
    {
        TEvent Filter(TEvent @event);
    }

    public interface IMapOperator<TIn, TOut> : IOperator 
        where TIn : class, IEvent
        where TOut : class, IEvent
    {
        IEnumerable<TOut> Map(TIn @event);
    }
}
