﻿using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.UnitTests.Events
{
    public class TestEvent2 : IEvent {
        
        public int Value { get; set; }
        public string Key { get; set; }

    }
}
