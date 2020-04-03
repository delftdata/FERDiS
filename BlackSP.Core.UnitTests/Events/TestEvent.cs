﻿using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.UnitTests.Events
{
    public class TestEvent : IEvent {
        
        public byte Value { get; set; }
        public string Key { get; set; }

        public DateTime EventTime { get; set; }
    }
}
