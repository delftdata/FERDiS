using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.OperatorSockets
{
    public class FilterOperatorSocket<TEvent> : OperatorSocketBase 
        where TEvent : class, IEvent
    {
        private readonly IFilterOperator<TEvent> _pluggedInOperator;

        public FilterOperatorSocket(IFilterOperator<TEvent> pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator;
        }

        protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));
            var typedEvent = @event as TEvent ?? throw new ArgumentException($"Argument \"{nameof(@event)}\" was of type {@event.GetType()}, expected: {typeof(TEvent)}");
            var output = _pluggedInOperator.Filter(typedEvent);
            if(output != null) //ie. the @event did not get filtered out
            {
                yield return output;
            }
        }

    }
}
