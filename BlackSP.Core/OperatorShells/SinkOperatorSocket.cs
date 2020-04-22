using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.OperatorShells
{
    public class SinkOperatorShell<TEvent> : OperatorShellBase 
        where TEvent : class, IEvent
    {
        private readonly ISinkOperator<TEvent> _pluggedInOperator;

        public SinkOperatorShell(ISinkOperator<TEvent> pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator;
        }

        protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));
            var tEvent = @event as TEvent ?? throw new ArgumentException($"Argument {nameof(@event)} was not of expected type {typeof(TEvent)}");
            //TODO: sink event into kafka? more generic?
            _pluggedInOperator.Sink(tEvent);
            return Enumerable.Empty<IEvent>();
        }

        

    }
}
