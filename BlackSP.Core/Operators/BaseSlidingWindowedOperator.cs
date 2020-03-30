using BlackSP.Core.Windows;
using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.Operators
{

    public abstract class BaseSlidingWindowedOperator : BaseOperator
    {
        private readonly IWindowedOperatorConfiguration _options;
        private readonly IDictionary<Type, SlidingEventWindow<IEvent>> _currentWindows;

        public BaseSlidingWindowedOperator(IWindowedOperatorConfiguration options) : base(options)
        {
            _options = options;
            _currentWindows = new Dictionary<Type, SlidingEventWindow<IEvent>>();
        }

        protected sealed override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));

            //TODO: implement custom exception
            var preInsertResults = PreWindowInsert(@event) ?? throw new Exception("PreWindowInsert returned null, expected IEnumerable");
            GetWindow(@event.GetType()).Add(@event);
            return preInsertResults;
        }

        /// <summary>
        /// Provides a handle for implementing pre-window-insertion logic<br/>
        /// A typical use would be to override this method to perform join logic
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        protected abstract IEnumerable<IEvent> PreWindowInsert(IEvent @event);


        protected SlidingEventWindow<IEvent> GetWindow(Type eventType)
        {
            if (_currentWindows.TryGetValue(eventType, out var eventWindow))
            {
                return eventWindow;
            }
            throw new Exception($"Missing type for sliding window, was trying to find ${eventType}");
        }
    }
}
