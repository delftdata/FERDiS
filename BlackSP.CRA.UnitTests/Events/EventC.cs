using BlackSP.Kernel.Events;
using System;

namespace BlackSP.CRA.UnitTests.Events
{
    public class EventC : IEvent
    {
        public string Key { get; set; }

        public DateTime EventTime { get; set; }
    }
}
