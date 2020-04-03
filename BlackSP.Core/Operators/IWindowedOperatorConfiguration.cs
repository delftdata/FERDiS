using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Operators
{
    public interface IWindowedOperatorConfiguration : IOperatorConfiguration
    {
        TimeSpan WindowSize { get; set; }
    }

    public interface IJoinOperatorConfiguration<TInA, TInB, TOut> : IWindowedOperatorConfiguration
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

    public interface IAggregateOperatorConfiguration<TIn, TOut> : IWindowedOperatorConfiguration
        where TIn : class, IEvent
        where TOut : class, IEvent

    {
        IEnumerable<TOut> Aggregate(IEnumerable<TIn> window);
    }
}
