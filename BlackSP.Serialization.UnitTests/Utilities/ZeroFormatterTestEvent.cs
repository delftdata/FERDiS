using BlackSP.Interfaces.Events;
using BlackSP.Serialization.Events;
using System;
using System.Collections.Generic;
using System.Text;
using ZeroFormatter;

namespace BlackSP.Serialization.UnitTests.Utilities
{
    [ZeroFormattable]
    public class ZeroFormatterTestEvent : BaseZeroFormattableEvent
    {
        [Index(1)]
        public virtual IDictionary<string, int> Values { get; set; }

        public ZeroFormatterTestEvent()
        {
        }

        public object GetValue()
        {
            return Values;
        }
    }
}
