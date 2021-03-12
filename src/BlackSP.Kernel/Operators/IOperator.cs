using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel.Operators
{
    public interface IOperator
    {
    }

    public interface ISourceOperator<TEvent> : IOperator
        where TEvent : class, IEvent
    {
        TEvent ProduceNext(CancellationToken t);
    }

    public interface ISinkOperator<TEvent> : IOperator
        where TEvent : class, IEvent
    {
        /// <summary>
        /// Last step in the streaming process, sink event into system output
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        Task Sink(TEvent @event);
    }

    public interface IFilterOperator<TEvent> : IOperator 
        where TEvent : class, IEvent
    {

        /// <summary>
        /// Event filter, return either the event or null
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        TEvent Filter(TEvent @event);
    }

    public interface IMapOperator<TIn, TOut> : IOperator 
        where TIn : class, IEvent
        where TOut : class, IEvent
    {
        IEnumerable<TOut> Map(TIn @event);
    }



    public interface ICycleOperator : IOperator
    {
        Task Consume(IEvent @event);
    }

    /// <summary>
    /// Interface to handle backchannel input over, backchannel input may not result in new messages.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public interface ICycleOperator<TEvent> : ICycleOperator
        where TEvent : class, IEvent
    {
        /// <summary>
        /// Handle to consume a message from upstream
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        Task Consume(TEvent @event);
    }
}
