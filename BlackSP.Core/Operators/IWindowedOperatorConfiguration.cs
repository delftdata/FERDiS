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
        IEvent Join(IEvent matchA, IEvent matchB);
    }

    public interface IAggregateOperatorConfiguration : IWindowedOperatorConfiguration
    {
        IEnumerable<IEvent> Aggregate(IEnumerable<IEvent> window);
    }
}
