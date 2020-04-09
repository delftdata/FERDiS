using BlackSP.Core.Windows;
using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.OperatorSockets
{

    public abstract class SlidingWindowedOperatorSocketBase : OperatorSocketBase
    {
        private readonly IWindowedOperator _pluggedInOperator;
        private readonly IDictionary<Type, SlidingEventWindow<IEvent>> _currentWindows;

        public SlidingWindowedOperatorSocketBase(IWindowedOperator pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator;
            _currentWindows = new Dictionary<Type, SlidingEventWindow<IEvent>>();
        }

        protected sealed override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));

            UpdateAllWindows(@event);

            //TODO: implement custom exception
            return OperateWithUpdatedWindows(@event) ?? throw new Exception("OperateWithUpdatedWindows returned null, expected IEnumerable");            
        }

        /// <summary>
        /// Handle for implementing post-window-insertion logic<br/>
        /// A typical use would be to override this method to perform join logic
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        protected abstract IEnumerable<IEvent> OperateWithUpdatedWindows(IEvent @event);

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
                _currentWindows.Add(eventType, new SlidingEventWindow<IEvent>(_pluggedInOperator.WindowSize));
            }
            if (_currentWindows.TryGetValue(eventType, out var eventWindow))
            {
                return eventWindow;
            }
            //TODO: custom exception
            throw new Exception($"Missing type for sliding window, was trying to find ${eventType}");
        }

        /// <summary>
        /// Inserts provided event in the right window and updates watermarks of any other
        /// </summary>
        /// <param name="event"></param>
        private void UpdateAllWindows(IEvent @event)
        {
            foreach(var window in _currentWindows)
            {
                var type = window.Key;
                var slidingWindow = window.Value;

                if(type == @event.GetType()) //if this window holds this type of event
                {
                    slidingWindow.Insert(@event); //then insert the event
                } 
                else if(slidingWindow.TryUpdateWatermark(@event)) //else if this event advances the watermark
                {
                    slidingWindow.Prune(); //prune any expired events from window
                }
            }
        }
    }
}
