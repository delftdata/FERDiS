using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Operators
{
    public interface IWindowedOperator : IOperator
    {
        TimeSpan WindowSize { get; }

        TimeSpan WindowSlideSize { get; }
    }

    public interface IJoinOperator<TInA, TInB, TOut> : IWindowedOperator
        where TInA : class, IEvent
        where TInB : class, IEvent
        where TOut : class, IEvent
    {
        /// <summary>
        /// Checks if two events are a match for a join or not
        /// </summary>
        /// <param name="testA"></param>
        /// <param name="testB"></param>
        /// <returns></returns>
        bool Match(TInA testA, TInB testB);

        /// <summary>
        /// This method performs the actual join of two events
        /// </summary>
        /// <param name="matchA"></param>
        /// <param name="matchB"></param>
        /// <returns></returns>
        TOut Join(TInA matchA, TInB matchB);
    }

    public interface IAggregateOperator<TIn, TOut> : IWindowedOperator
        where TIn : class, IEvent
        where TOut : class, IEvent

    {

        /// <summary>
        /// Closing of a window, aggregates into an enumerable of the expected output type
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        IEnumerable<TOut> Aggregate(IEnumerable<TIn> window);
    }
}
