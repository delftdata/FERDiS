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

        protected override IEnumerable<IEvent> OperateWithUpdatedWindows(IEvent newEvent, IEnumerable<IEvent> closedWindow, bool windowDidAdvance)
        {
            _ = closedWindow ?? throw new ArgumentNullException(nameof(closedWindow));

            if(!windowDidAdvance)
            {
                return Enumerable.Empty<IEvent>();
            }

            if(_pluggedInOperator.WindowSize == _pluggedInOperator.WindowSlideSize)
            {
                //tumbling mode
                return _pluggedInOperator.Aggregate(closedWindow.Cast<TIn>());
            }
            else
            {
                //sliding mode
                var currentWindowContent = GetWindow(typeof(TIn)).Events.Cast<TIn>();
                return _pluggedInOperator.Aggregate(currentWindowContent);
            }
        }
    }
}
