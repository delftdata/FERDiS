using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.Operators
{
    public abstract class BaseSlidingWindowedOperator : BaseOperator
    {
        protected IEnumerable<KeyValuePair<DateTime, IEvent>> CurrentWindow => _currentWindow;

        private IEnumerable<KeyValuePair<DateTime, IEvent>> _currentWindow;
        private readonly IWindowedOperatorConfiguration _options;

        public BaseSlidingWindowedOperator(IWindowedOperatorConfiguration options) : base(options)
        {
            _options = options;
            _currentWindow = new Dictionary<DateTime, IEvent>();
        }

        protected sealed override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            //TODO: implement custom exception
            var preInsertResults = PreWindowInsert(@event) ?? throw new Exception("PreWindowInsert returned null, expected IEnumerable");

            _currentWindow = _currentWindow.Where(pair => pair.Key > DateTime.Now); //removes expired events

            //insert new event in window
            var expiresAt = DateTime.Now.AddMilliseconds(_options.WindowSize.TotalMilliseconds);
            _currentWindow.Append(new KeyValuePair<DateTime, IEvent>(expiresAt, @event));

            return preInsertResults;
        }

        /// <summary>
        /// Provides a handle for implementing pre-window-insertion logic<br/>
        /// A typical use would be to override this method to perform join logic<br/>
        /// When overriding, do not invoke base as default havior is to return an empty enumerable
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        protected abstract IEnumerable<IEvent> PreWindowInsert(IEvent @event);
    }
}
