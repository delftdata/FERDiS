using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.OperatorSockets
{
    public class SinkOperatorSocket<TEvent> : OperatorSocketBase 
        where TEvent : class, IEvent
    {
        private readonly ISinkOperator<TEvent> _pluggedInOperator;

        public SinkOperatorSocket(ISinkOperator<TEvent> pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator;
        }

        protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            //TODO: sink event into kafka? more generic?

            return Enumerable.Empty<IEvent>();
        }

        

    }
}
