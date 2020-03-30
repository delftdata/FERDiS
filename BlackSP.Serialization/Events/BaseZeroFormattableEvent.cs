using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Text;
using ZeroFormatter;

namespace BlackSP.Serialization.Events
{
    /// <summary>
    /// Provides an abstraction base for events compatible with 
    /// the ZeroFormatter serializer
    /// </summary>
    [DynamicUnion]
    public abstract class BaseZeroFormattableEvent : IEvent
    {
        [Index(0)]
        public virtual string Key { get; set; }

        [Index(1)]
        public virtual DateTime EventTime { get; set; }
    }
}
