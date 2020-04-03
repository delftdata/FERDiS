using BlackSP.Core.Windows;
using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Operators
{
    public abstract class BaseWindowedOperator<TIn, TOut> : BaseOperator
        where TIn : class, IEvent
        where TOut : class, IEvent
    {

        private readonly IWindowedOperatorConfiguration _options;
        private FixedEventWindow<TIn> _currentWindow;
        
        public BaseWindowedOperator(IWindowedOperatorConfiguration options) : base(options)
        {
            _options = options;            
        }

        public override Task Start(DateTime at)
        {
            _currentWindow = new FixedEventWindow<TIn>(at, _options.WindowSize);
            return base.Start(at);
        }

        protected sealed override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));
            var typedEvent = @event as TIn ?? throw new ArgumentException($"Argument {nameof(@event)} was of type {@event.GetType()}, expected: {typeof(TIn)}");

            var closedWindow = _currentWindow.Insert(typedEvent);
            return !closedWindow.Any() ? Enumerable.Empty<IEvent>() : ProcessClosedWindow(closedWindow)
                ?? throw new Exception("ProcessClosedWindow returned null, expected IEnumerable");
        }

        protected abstract IEnumerable<TOut> ProcessClosedWindow(IEnumerable<TIn> closedWindow);

        /*
        /// <summary>
        /// Gets invoked by timer and is responsible for:<br/>
        /// 1. Closing the current window.<br/>
        /// 2. Invoking subclass method processing the window.<br/>
        /// 3. Enqueueing the output as this method gets invoked outside the base processing loop
        /// </summary>
        /// <param name="_"></param>
        private void OnWindowExpiredTimerTick(object _)
        {
            if(CancellationToken.IsCancellationRequested)
            {
                _windowTimer.Change(int.MaxValue, int.MaxValue); //stop ticking (disposing is handled by IDisposable)
                return; //stop processing if cancelled
            }



            var previousWindow = _currentWindow.Find();
            //TODO: log diagnostics/debug stuff?
            //TODO: make custom exception
            var operatorOutput = ProcessClosedWindow(previousWindow) 
                ?? throw new Exception("ProcessClosedWindow returned null, expected IEnumerable");
            
            
            EgressOutputEvents(operatorOutput);
        }*/

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
