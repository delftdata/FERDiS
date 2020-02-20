using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Operators
{
    //before/after are more checkpointing delegates though.. maybe not needed as not user defined?
    //public delegate void OnBeforeEvent(IEvent target);
    //public delegate void OnAfterEvent(IEvent target);

    public delegate TOutput OnEvent<TInput, TOutput>(TInput target);

    public interface IOperatorConfiguration
    {
        int? OutputEndpointCount { get; set; }
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
