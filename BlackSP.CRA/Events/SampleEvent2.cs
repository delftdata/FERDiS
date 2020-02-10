using BlackSP.Serialization.Events;
using ZeroFormatter;

namespace BlackSP.CRA.Events
{
    [ZeroFormattable]
    public class SampleEvent2 : BaseZeroFormattableEvent
    {
        [Index(1)]
        public virtual string Value2 { get; set; }
    }
}
