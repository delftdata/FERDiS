using BlackSP.Interfaces.Events;
using BlackSP.Serialization.Events;
using ProtoBuf;
using ZeroFormatter;

namespace BlackSP.CRA.Events
{
    [ProtoContract]
    public class SampleEvent : IEvent
    {
        [ProtoMember(1)]
        public string Key { get; set; }

        [ProtoMember(2)]
        public string Value { get; set; }
    }
}
