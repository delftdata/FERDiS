using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Operators
{
    public interface IWindowedOperatorConfiguration : IOperatorConfiguration
    {
        TimeSpan WindowSize { get; set; }
    }

    public interface IJoinOperatorConfiguration : IWindowedOperatorConfiguration
    {
        /// <summary>
        /// Checks if two events are a match for a join or not
        /// </summary>
        /// <param name="testA"></param>
        /// <param name="testB"></param>
        /// <returns></returns>
        bool Match(IEvent testA, IEvent testB);

        /// <summary>
        /// This method performs the actual join of two events
        /// </summary>
        /// <param name="matchA"></param>
        /// <param name="matchB"></param>
        /// <returns></returns>
        IEvent Join(IEvent matchA, IEvent matchB);
    }

    public interface IAggregateOperatorConfiguration<TIn, TOut> : IWindowedOperatorConfiguration
        where TIn : class, IEvent
        where TOut : class, IEvent

    {
        IEnumerable<TOut> Aggregate(IEnumerable<TIn> window);
    }
}
