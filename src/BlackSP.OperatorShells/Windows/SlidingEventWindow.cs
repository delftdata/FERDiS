using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.OperatorShells.Windows
{
    [Serializable]
    public class SlidingEventWindow<TEvent> : EventWindowBase<TEvent>
        where TEvent : class, IEvent
    {
        public SlidingEventWindow(DateTime startDate, TimeSpan windowSize, TimeSpan windowSlideSize) : base(startDate, windowSize, windowSlideSize)
        {
        }

        /// <summary>
        /// Returns the window content and prunes events that should be removed from the window
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<TEvent> OnWindowAdvanced()
        {
            var windowContent = Events.ToArray();
            Prune();
            return windowContent;
        }

        /// <summary>
        /// Removes events from the current window based on processing time<br/>
        /// (anything that slid out will be removed)</br>
        /// </summary>
        public void Prune()
        {
            var maxAge = CurrentWindowStart.Ticks;

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
