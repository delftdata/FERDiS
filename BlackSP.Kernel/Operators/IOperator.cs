using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Kernel.Operators
{
    public interface IOperator
    {
    }

    public interface ISourceOperator<TEvent> : IOperator
        where TEvent : class, IEvent
    {
        string KafkaTopicName { get; }

        IEnumerable<TEvent> GetTestEvents();
    }

    public interface ISinkOperator<TEvent> : IOperator
        where TEvent : class, IEvent
    {
        string KafkaTopicName { get; }

        /// <summary>
        /// Last step in the streaming process, emits event to external system
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        Task Sink(TEvent @event);
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
