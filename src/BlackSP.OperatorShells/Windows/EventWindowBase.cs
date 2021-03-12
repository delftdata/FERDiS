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
        
        /// <summary>
        /// The size of the window
        /// </summary>
        protected TimeSpan WindowSize { get; private set; }
        
        /// <summary>
        /// The amount of time the window slides ahead<br/>
        /// Note: when equal to WindowSize, tumbling behavior will emerge<br/>
        /// When lower than WindowSize, sliding behavior will emerge
        /// </summary>
        protected TimeSpan WindowSlideSize { get; private set; }

        /// <summary>
        /// Represents a local timestamp (processing time) on which the current window has opened
        /// </summary>
        protected DateTime CurrentWindowStart { get; set; }

        private readonly object _windowLock;
        
        public EventWindowBase(DateTime startDate, TimeSpan windowSize, TimeSpan windowSlideSize)
        {
            WindowSize = windowSize == default ? throw new ArgumentException($"{nameof(windowSize)} has default value, pass a valid TimeSpan") : windowSize;
            WindowSlideSize = windowSlideSize == default ? throw new ArgumentException($"{nameof(windowSlideSize)} has default value, pass a valid TimeSpan") : windowSlideSize;

            SortedEvents = new SortedList<long, TEvent>();
            CurrentWindowStart = startDate;
            _windowLock = new object();
        }

        /// <summary>
        /// Inserts an event in the window and updates watermarks, may<br/>
        /// a. return a closed window or <br/>
        /// b. return whats left in the window <br/>
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public IEnumerable<TEvent> Insert(TEvent @event, DateTime processingTime, out bool windowDidAdvance)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));

            lock (_windowLock)
            {
                windowDidAdvance = AdvanceWindow(processingTime);
                var events =  windowDidAdvance ? OnWindowAdvanced() : Enumerable.Empty<TEvent>();
                //add after window advance
                SafeAddEventToWindow(processingTime.Ticks, @event);
                return events;
            }
        }

        /// <summary>
        /// Advances processing time
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public bool AdvanceWindow(DateTime newProcessingTime)
        {
            bool res = false;
            while(newProcessingTime.Ticks > CurrentWindowStart.Ticks + WindowSize.Ticks)
            {
                CurrentWindowStart = CurrentWindowStart.Add(WindowSlideSize);
                res = true;
            }
            return res;
        }

        /// <summary>
        /// Handle for implementing different window behaviors<br/>
        /// Can return values in case a window closed
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<TEvent> OnWindowAdvanced();


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
