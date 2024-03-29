﻿using BlackSP.Checkpointing;
using BlackSP.OperatorShells.Windows;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.OperatorShells
{

    public abstract class SlidingWindowedOperatorShellBase : OperatorShellBase
    {
        private readonly IWindowedOperator _pluggedInOperator;

        [ApplicationState]
        private readonly IDictionary<string, SlidingEventWindow<IEvent>> _currentWindows;

        public SlidingWindowedOperatorShellBase(IWindowedOperator pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator ?? throw new ArgumentNullException(nameof(pluggedInOperator));
            _currentWindows = new Dictionary<string, SlidingEventWindow<IEvent>>();
        }

        public sealed override async Task<IEnumerable<IEvent>> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));
            await Task.Yield();
            var closedWindow = UpdateAllWindows(@event, out bool windowDidAdvance);
            return OperateWithUpdatedWindows(@event, closedWindow, windowDidAdvance) ?? throw new Exception("OperateWithUpdatedWindows returned null, expected IEnumerable");            
        }

        /// <summary>
        /// Handle for implementing post-window-insertion logic<br/>
        /// A typical use would be to override this method to perform join logic
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        protected abstract IEnumerable<IEvent> OperateWithUpdatedWindows(IEvent newEvent, IEnumerable<IEvent> closedWindow, bool windowDidAdvance);

        /// <summary>
        /// Handle for subclasses to access the sliding window with events of provided type
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        protected SlidingEventWindow<IEvent> GetWindow(Type eventType)
        {
            _ = eventType ?? throw new ArgumentNullException(nameof(eventType));
            var eventKey = eventType.FullName;
            if (!_currentWindows.ContainsKey(eventKey))
            {
                _currentWindows.Add(eventKey, new SlidingEventWindow<IEvent>(DateTime.Now, _pluggedInOperator.WindowSize, _pluggedInOperator.WindowSlideSize));
            }
            if (_currentWindows.TryGetValue(eventKey, out var eventWindow))
            {
                return eventWindow;
            }
            //TODO: custom exception
            throw new Exception($"Missing sliding window of type {eventType}");
        }

        /// <summary>
        /// Inserts provided event in the right window
        /// </summary>
        /// <param name="event"></param>
        private IEnumerable<IEvent> UpdateAllWindows(IEvent @event, out bool windowDidAdvance)
        {
            windowDidAdvance = false;
            var closedWindow = Enumerable.Empty<IEvent>();
            foreach(var window in _currentWindows)
            {
                var type = window.Key;
                var slidingWindow = window.Value;

                if(type == @event.GetType().FullName) //if this window holds this type of event
                {
                    closedWindow = slidingWindow.Insert(@event, DateTime.Now, out windowDidAdvance); //then insert the event
                } 
                else if(slidingWindow.AdvanceWindow(DateTime.Now)) //else (other window type) if this event advances the window
                {
                    slidingWindow.Prune(); //prune any expired events from window
                }
            }
            return closedWindow;
        }
    }
}
