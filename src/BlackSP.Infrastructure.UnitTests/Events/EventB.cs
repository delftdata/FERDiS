using BlackSP.Kernel.Events;
using System;

namespace BlackSP.CRA.UnitTests.Events
{
    public class EventB : IEvent
    {
        public string Key { get; set; }

        public DateTime EventTime { get; set; }
    }
}
