using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.OperatorShells
{
    public class AggregateOperatorShell<TIn, TOut> : WindowedOperatorShellBase<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {

        private readonly IAggregateOperator<TIn, TOut> _pluggedInOperator;

        public AggregateOperatorShell(IAggregateOperator<TIn, TOut> pluggedInOperator) : base(pluggedInOperator)
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
