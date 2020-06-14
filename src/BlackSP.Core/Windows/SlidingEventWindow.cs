using BlackSP.Kernel.Models;
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

        protected override IEnumerable<TEvent> OnWaterMarkAdvanced()
        {
            Prune();
            return Enumerable.Empty<TEvent>();
        }

        /// <summary>
        /// Removes all events from the current window based on watermark.<br/>
        /// (anything that slid out will be removed)</br>
        /// Wont perform any pruning when there is nothing to prune
        /// </summary>
        /// <param name="maxAge"></param>
        public void Prune()
        {
            var maxAge = LatestEventTime - WindowSize.Ticks;

            var oldestEventTick = SortedEvents.FirstOrDefault().Key;
            if (oldestEventTick == default || oldestEventTick > maxAge)
            {   //only prune if we know there is something to prune
                return;
            }

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
