using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.Windows
{
    public class FixedEventWindow<TEvent> : EventWindowBase<TEvent>
        where TEvent : class, IEvent
    {
        private long LowerBoundary { get; set; }
        private long UpperBoundary => LowerBoundary + WindowSize.Ticks;

        public FixedEventWindow(DateTime startTime, TimeSpan windowSize) : base(windowSize)
        {
            LowerBoundary = startTime.Ticks;
        }

        protected override IEnumerable<TEvent> OnWaterMarkAdvanced()
        {
            if(LatestEventTime >= UpperBoundary)
            {   //the window has closed as events beyond the upper boundary are arriving..
                //clear the window and set the new window boundaries
                var closedWindow = Events.ToArray();
                SortedEvents.Clear();
                LowerBoundary = UpperBoundary;
                return closedWindow;
            }
            return Enumerable.Empty<TEvent>();
        }
    }
}
