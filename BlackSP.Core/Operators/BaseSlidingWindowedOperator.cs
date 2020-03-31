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
            
            GetWindow(@event.GetType()).Add(@event);
            
            //TODO: implement custom exception
            return PreWindowInsert(@event) ?? throw new Exception("PreWindowInsert returned null, expected IEnumerable");            
        }

        /// <summary>
        /// Provides a handle for implementing pre-window-insertion logic<br/>
        /// A typical use would be to override this method to perform join logic
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        protected abstract IEnumerable<IEvent> PreWindowInsert(IEvent @event);//TODO: rename this

        /// <summary>
        /// Handle for subclasses to access the sliding window with events of provided type
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        protected SlidingEventWindow<IEvent> GetWindow(Type eventType)
        {
            _ = eventType ?? throw new ArgumentNullException(nameof(eventType));
            if(!_currentWindows.ContainsKey(eventType))
            {
                _currentWindows.Add(eventType, new SlidingEventWindow<IEvent>(_options.WindowSize));
            }
            if (_currentWindows.TryGetValue(eventType, out var eventWindow))
            {
                return eventWindow;
            }
            throw new Exception($"Missing type for sliding window, was trying to find ${eventType}");
        }
    }
}
