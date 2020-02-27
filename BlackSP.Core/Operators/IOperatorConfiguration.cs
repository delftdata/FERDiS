using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Operators
{
    public interface IOperatorConfiguration
    {
    }

    public interface IFilterOperatorConfiguration : IOperatorConfiguration
    {
        IEvent Filter(IEvent @event);
    }

    public interface IMapOperatorConfiguration : IOperatorConfiguration
    {
        IEnumerable<IEvent> Map(IEvent @event);
    }
}
