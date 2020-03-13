using BlackSP.Interfaces.Events;
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
        private IEnumerable<TIn> _currentWindow;
        private Timer _windowTimer;
        private object _windowLock;
        public BaseWindowedOperator(IWindowedOperatorConfiguration options) : base(options)
        {
            _options = options;
            _currentWindow = new List<TIn>();
            _windowTimer = null;
            _windowLock = new object();
        }

        public override Task Start()
        {
            //Start timer that will keep closing windows
            _windowTimer = new Timer(OnWindowExpiredTimerTick, null, _options.WindowSize, _options.WindowSize);
            return base.Start();
        }

        protected sealed override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));
            var typedEvent = @event as TIn ?? throw new ArgumentException($"Argument {nameof(@event)} was of type {@event.GetType()}, expected: {typeof(TIn)}");
            lock (_windowLock)
            {
                _currentWindow = _currentWindow.Append(typedEvent);
            }
            return Enumerable.Empty<IEvent>();
        }

        protected abstract IEnumerable<TOut> ProcessClosedWindow(IEnumerable<TIn> closedWindow);

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

            var previousWindow = CloseCurrentWindow();
            //TODO: log diagnostics/debug stuff?
            //TODO: make custom exception
            var operatorOutput = ProcessClosedWindow(previousWindow) 
                ?? throw new Exception("ProcessClosedWindow returned null, expected IEnumerable");
            
            
            EgressOutputEvents(operatorOutput);
        }

        private IEnumerable<TIn> CloseCurrentWindow()
        {
            lock(_windowLock)
            {
                var eventsInWindow = _currentWindow.ToArray();
                _currentWindow = new List<TIn>();
                return eventsInWindow;
            }
            
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _windowTimer?.Dispose();
                }
                disposedValue = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
