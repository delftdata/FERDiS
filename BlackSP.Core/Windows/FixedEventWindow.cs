using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.Windows
{
    public class FixedEventWindow<TEvent> : EventWindowBase<TEvent>
        where TEvent : class, IEvent
    {
        private DateTime LowerBoundary { get; set; }
        private DateTime UpperBoundary => LowerBoundary + WindowSize;

        public FixedEventWindow(DateTime startTime, TimeSpan windowSize) : base(windowSize)
        {
            LowerBoundary = startTime;
        }

        protected override IEnumerable<TEvent> OnWaterMarkAdvanced(DateTime newWatermark)
        {
            if(newWatermark >= UpperBoundary)
            {   //the window has closed as events beyond the upper boundary are arriving..
                //clear the window and set the new window boundaries
                var closedWindow = Events.ToArray();
                SortedEvents.Clear();
                LowerBoundary = UpperBoundary; //TODO: test condition
                return closedWindow;
            }
            return Enumerable.Empty<TEvent>();
        }
    }
}
