using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Core.Windows
{
    [Serializable]
    public abstract class EventWindowBase<TEvent>
        where TEvent: class, IEvent
    {

        public ICollection<TEvent> Events => SortedEvents.Values;
        protected SortedList<long, TEvent> SortedEvents { get; private set; }
        protected TimeSpan WindowSize { get; private set; }
        
        /// <summary>
        /// Represents a watermark, a lower bound to determine which events to discard from the window
        /// </summary>
        protected long LatestEventTime { get; set; }

        private readonly object _windowLock;
        
        public EventWindowBase(TimeSpan windowSize)
        {
            _ = windowSize == default ? throw new ArgumentException($"{nameof(windowSize)} has default value, pass a valid TimeSpan") : windowSize;

            SortedEvents = new SortedList<long, TEvent>();
            LatestEventTime = DateTime.MinValue.Ticks;
            WindowSize = windowSize;
            _windowLock = new object();
        }

        /// <summary>
        /// Inserts an event in the window and updates watermarks, may<br/>
        /// a. return a closed window or <br/>
        /// b. drop expired events from the public Events collection
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public IEnumerable<TEvent> Insert(TEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));

            lock (_windowLock)
            {
                long eventTimeTicks = @event.EventTime.Ticks;
                var events = TryUpdateWatermark(@event) 
                    ? OnWaterMarkAdvanced()
                    : Enumerable.Empty<TEvent>();
                
                SafeAddEventToWindow(eventTimeTicks, @event);
                return events;
            }
        }

        /// <summary>
        /// Handle for updating the watermark of the current window.<br/>
        /// Only updates if the provided event watermark is larger than the current watermark
        /// NOTE: NOT ACTUAL WATERMARKS, NEEDS REFACTORING
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public bool TryUpdateWatermark(TEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));

            long eventTimeTicks = @event.EventTime.Ticks;
            if(eventTimeTicks > LatestEventTime)
            {
                LatestEventTime = eventTimeTicks;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handle for implementing different window behaviors<br/>
        /// Can return values in case a window closed<br/>
        /// Gets invoked BEFORE adding the event with the advanced watermark to the current window
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<TEvent> OnWaterMarkAdvanced();


        /// <summary>
        /// Add an event to the current sortedlist that is the window using a safe strategy.<br/>
        /// When running into a duplicated key it will attempt to move the older (existing) event
        /// one Tick (1/10millionth of a second) back in time to free up the duplicate key.<br/>
        /// Will do this recursively to ensure no duplicate key exception can occur.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="event"></param>
        private void SafeAddEventToWindow(long key, TEvent @event)
        {
            if (SortedEvents.ContainsKey(key) && SortedEvents.TryGetValue(key, out var eventFromWindow))
            {
                SortedEvents.Remove(key);
                SafeAddEventToWindow(key - 1, eventFromWindow);
            }
            SortedEvents.Add(key, @event);
        }
    }
}
