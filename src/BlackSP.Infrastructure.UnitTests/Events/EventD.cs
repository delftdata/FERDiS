﻿using BlackSP.Kernel.Models;
using System;

namespace BlackSP.CRA.UnitTests.Events
{
    public class EventD : IEvent
    {
        public string Key { get; set; }

        public DateTime EventTime { get; set; }

        public int GetPartitionKey()
        {
            return Key.GetHashCode();
        }
    }
}
