using BlackSP.Checkpointing;
using BlackSP.Core.Windows;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.OperatorShells
{
    public abstract class WindowedOperatorShellBase<TIn, TOut> : OperatorShellBase
        where TIn : class, IEvent
        where TOut : class, IEvent
    {

        private readonly IWindowedOperator _pluggedInOperator;

        [Checkpointable]
        private FixedEventWindow<TIn> _currentWindow;
        
        public WindowedOperatorShellBase(IWindowedOperator pluggedInOperator) : base()
        {
            _pluggedInOperator = pluggedInOperator ?? throw new ArgumentNullException(nameof(pluggedInOperator));
            _currentWindow = new FixedEventWindow<TIn>(DateTime.UtcNow, _pluggedInOperator.WindowSize);
        }

        public sealed override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));
            var typedEvent = @event as TIn ?? throw new ArgumentException($"Argument {nameof(@event)} was of type {@event.GetType()}, expected: {typeof(TIn)}");

            var closedWindow = _currentWindow.Insert(typedEvent);
            return !closedWindow.Any() ? Enumerable.Empty<IEvent>() : ProcessClosedWindow(closedWindow)
                ?? throw new Exception("ProcessClosedWindow returned null, expected IEnumerable");
        }

        protected abstract IEnumerable<TOut> ProcessClosedWindow(IEnumerable<TIn> closedWindow);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //_windowTimer?.Dispose();
                }
                disposedValue = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
