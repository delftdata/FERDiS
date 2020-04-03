using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.Windows
{
    public class SlidingEventWindow<TEvent> : EventWindowBase<TEvent>
        where TEvent : class, IEvent
    {
        public SlidingEventWindow(TimeSpan windowSize) : base(windowSize)
        {
        }

        protected override IEnumerable<TEvent> OnWaterMarkAdvanced(long newWatermark)
        {
            var oldestEventTick = SortedEvents.FirstOrDefault().Key;
            var lowerBoundary = newWatermark - WindowSize.Ticks;

            if (oldestEventTick != default && oldestEventTick <= lowerBoundary)
            {   //only prune if we know there is something to prune
                Prune(lowerBoundary);
            }
            return Enumerable.Empty<TEvent>();
        }

        /// <summary>
        /// Removes all events from the current window older than the provided datetime
        /// </summary>
        /// <param name="maxAge"></param>
        private void Prune(long maxAge)
        {
            var eventKeys = SortedEvents.Keys.ToList();
            int expiredIndex = eventKeys.BinarySearch(maxAge);
            //either the element with maxAge exists (unlikely) or we get the next bigger index as bitwise complement
            expiredIndex = expiredIndex < 0 ? (~expiredIndex - 1) : expiredIndex; 
            foreach (var expiredEventKey in eventKeys.Take(expiredIndex + 1))
            {
                SortedEvents.Remove(expiredEventKey);
            }
        }
    }
}
