using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BlackSP.OperatorShells
{
    public class AggregateOperatorShell<TIn, TOut> : SlidingWindowedOperatorShellBase
        where TIn : class, IEvent
        where TOut : class, IEvent
    {
        private readonly IAggregateOperator<TIn, TOut> _pluggedInOperator;
        private readonly ILogger _logger;

        public AggregateOperatorShell(IAggregateOperator<TIn, TOut> pluggedInOperator, ILogger logger) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator ?? throw new ArgumentNullException(nameof(pluggedInOperator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            GetWindow(typeof(TIn)); //ensure window creation
        }

        protected override IEnumerable<IEvent> OperateWithUpdatedWindows(IEvent newEvent, IEnumerable<IEvent> closedWindow, bool windowDidAdvance)
        {
            _ = closedWindow ?? throw new ArgumentNullException(nameof(closedWindow));

            if(!windowDidAdvance)
            {
                return Enumerable.Empty<IEvent>();
            }

            var sw = new Stopwatch();
            int count = 0;
            try
            {
                sw.Start();
                if (_pluggedInOperator.WindowSize == _pluggedInOperator.WindowSlideSize)
                {
                    //tumbling mode
                    count = closedWindow.Count();
                    return _pluggedInOperator.Aggregate(closedWindow.Cast<TIn>());
                }
                else
                {
                    //sliding mode
                    var currentWindowContent = GetWindow(typeof(TIn)).Events.Cast<TIn>();
                    count = currentWindowContent.Count();
                    return _pluggedInOperator.Aggregate(currentWindowContent);
                }
            } 
            finally
            {
                sw.Stop();
                _logger.Verbose($"Processed window of size {count} in {sw.ElapsedTicks} ticks");
            }
        }
    }
}
