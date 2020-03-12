using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Operators
{
    public abstract class BaseWindowedOperator : BaseOperator
    {

        private readonly IWindowedOperatorConfiguration _options;
        private IEnumerable<IEvent> _currentWindow;
        private Timer _windowTimer;

        public BaseWindowedOperator(IWindowedOperatorConfiguration options) : base(options)
        {
            _options = options;
            _currentWindow = new List<IEvent>();
            _windowTimer = null;
        }

        public override Task Start()
        {
            //Start timer that will keep closing windows
            _windowTimer = new Timer(OnWindowExpiredTimerTick, null, TimeSpan.FromSeconds(0), _options.WindowSize);
            return base.Start();
        }

        protected sealed override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            _currentWindow.Append(@event);
            return Enumerable.Empty<IEvent>();
        }

        protected abstract IEnumerable<IEvent> ProcessClosedWindow(IEnumerable<IEvent> closedWindow);

        /// <summary>
        /// Gets invoked by timer and is responsible for:<br/>
        /// 1. Closing the current window.<br/>
        /// 2. Invoking subclass method processing the window.<br/>
        /// 3. Enqueueing the output as this method gets invoked outside the base processing loop
        /// </summary>
        /// <param name="_"></param>
        private void OnWindowExpiredTimerTick(object _)
        {
            var previousWindow = CloseCurrentWindow();
            //TODO: make custom exception
            var operatorOutput = ProcessClosedWindow(previousWindow) ?? throw new Exception("ProcessClosedWindow returned null, expected IEnumerable");
            EgressOutputEvents(operatorOutput);
        }

        private IEnumerable<IEvent> CloseCurrentWindow()
        {
            //TODO: check if not overwriting eventsInWindow with new list
            var eventsInWindow = _currentWindow.ToArray();
            _currentWindow = new List<IEvent>();
            return eventsInWindow;
        }
    }
}
