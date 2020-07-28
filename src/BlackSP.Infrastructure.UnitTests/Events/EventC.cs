﻿using BlackSP.Kernel.Models;
using System;

namespace BlackSP.Infrastructure.UnitTests.Events
{
    public class EventC : IEvent
    {
        public string Key { get; set; }

        public DateTime EventTime { get; set; }

        public int GetPartitionKey()
        {
            return Key.GetHashCode();
        }
    }
}
