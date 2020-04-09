using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.OperatorSockets
{
    public class AggregateOperatorSocket<TIn, TOut> : WindowedOperatorSocketBase<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {

        private readonly IAggregateOperator<TIn, TOut> _pluggedInOperator;

        public AggregateOperatorSocket(IAggregateOperator<TIn, TOut> pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator;
        }

        protected override IEnumerable<TOut> ProcessClosedWindow(IEnumerable<TIn> closedWindow)
        {
            _ = closedWindow ?? throw new ArgumentNullException(nameof(closedWindow));
            return _pluggedInOperator.Aggregate(closedWindow);
        }
    }
}
