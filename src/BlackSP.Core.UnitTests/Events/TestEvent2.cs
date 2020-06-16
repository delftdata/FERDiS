using BlackSP.Kernel.Models;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.UnitTests.Events
{
    public class TestEvent2 : IEvent {
        
        public int Value { get; set; }
        public string Key { get; set; }

        public DateTime EventTime { get; set; }

        public int GetPartitionKey()
        {
            return Key.GetHashCode();
        }
    }
}
