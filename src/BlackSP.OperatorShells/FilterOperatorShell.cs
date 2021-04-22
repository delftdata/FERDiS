using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.OperatorShells
{
    public class FilterOperatorShell<TEvent> : OperatorShellBase 
        where TEvent : class, IEvent
    {
        private readonly IFilterOperator<TEvent> _pluggedInOperator;

        public FilterOperatorShell(IFilterOperator<TEvent> pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator ?? throw new ArgumentNullException(nameof(pluggedInOperator));
        }

        public override Task<IEnumerable<IEvent>> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));
            var typedEvent = @event as TEvent ?? throw new ArgumentException($"Argument \"{nameof(@event)}\" was of type {@event.GetType()}, expected: {typeof(TEvent)}");
            var output = _pluggedInOperator.Filter(typedEvent);
            return Task.FromResult(output.YieldOrEmpty().Cast<IEvent>());
        }

    }
}
