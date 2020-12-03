using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.OperatorShells
{
    public class AggregateOperatorShell<TIn, TOut> : SlidingWindowedOperatorShellBase
        where TIn : class, IEvent
        where TOut : class, IEvent
    {
        private readonly IAggregateOperator<TIn, TOut> _pluggedInOperator;

        public AggregateOperatorShell(IAggregateOperator<TIn, TOut> pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator ?? throw new ArgumentNullException(nameof(pluggedInOperator));

            GetWindow(typeof(TIn)); //ensure window creation
        }

        protected override IEnumerable<IEvent> OperateWithUpdatedWindows(IEvent newEvent, IEnumerable<IEvent> closedWindow)
        {
            _ = closedWindow ?? throw new ArgumentNullException(nameof(closedWindow));
            if(!closedWindow.Any())
            {
                return closedWindow;
            }
            return _pluggedInOperator.Aggregate(closedWindow.Cast<TIn>());
        }
    }
}
