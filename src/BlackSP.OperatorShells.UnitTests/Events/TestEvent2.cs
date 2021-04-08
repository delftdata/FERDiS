using BlackSP.Kernel.Models;
using System;

namespace BlackSP.Core.UnitTests.Events
{
    public class TestEvent2 : IEvent {
        
        public int Value { get; set; }
        public int? Key { get; set; }

        public DateTime EventTime { get; set; }
    }
}
