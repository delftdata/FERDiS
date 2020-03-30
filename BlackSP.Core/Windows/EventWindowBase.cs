using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Core.Windows
{
    public abstract class EventWindowBase<TEvent>
        where TEvent: class, IEvent
    {

        public ICollection<TEvent> Events => SortedEvents.Values;
        protected SortedList<DateTime, TEvent> SortedEvents { get; private set; }
        protected TimeSpan WindowSize { get; private set; }
        private DateTime LatestEventTime { get; set; }

        private readonly object _windowLock;
        
        public EventWindowBase(TimeSpan windowSize)
        {
            SortedEvents = new SortedList<DateTime, TEvent>();
            LatestEventTime = DateTime.MinValue;
            WindowSize = windowSize;
            _windowLock = new object();
        }

        public IEnumerable<TEvent> Add(TEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));

            lock (_windowLock)
            {
                var events = @event.EventTime > LatestEventTime 
                    ? OnWaterMarkAdvanced(LatestEventTime = @event.EventTime)
                    : Enumerable.Empty<TEvent>();
                
                SafeAddEventToWindow(@event.EventTime, @event);
                return events;
            }
        }

        /// <summary>
        /// Handle for implementing different window behaviors<br/>
        /// Can return values in case a window closed<br/>
        /// Gets invoked BEFORE adding the event with the advanced watermark to the current window
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<TEvent> OnWaterMarkAdvanced(DateTime newWatermark);


        private void SafeAddEventToWindow(DateTime key, TEvent @event)
        {
            //if already seen this exact datetime just add miliseconds untill we find a free key
            if (SortedEvents.ContainsKey(key))
            {   
                SafeAddEventToWindow(key.AddMilliseconds(-1), @event);
                return;
            }
            SortedEvents.Add(key, @event);
        }
    }
}
