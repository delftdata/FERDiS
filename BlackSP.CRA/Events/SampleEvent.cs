using BlackSP.Interfaces.Events;
using BlackSP.Serialization.Events;
using ZeroFormatter;

namespace BlackSP.CRA.Events
{
    [ZeroFormattable]
    public class SampleEvent : BaseZeroFormattableEvent
    {
        [Index(1)]
        public string Value { get; set; }
    }
}
