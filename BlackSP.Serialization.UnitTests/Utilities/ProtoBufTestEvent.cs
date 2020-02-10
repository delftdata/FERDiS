using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Interfaces.Events;
using ProtoBuf;
namespace BlackSP.Serialization.UnitTests.Utilities
{
    [ProtoContract]
    public class ProtoBufTestEvent : IEvent
    {
        [ProtoMember(1)]
        public string Key { get; set; }
        [ProtoMember(2)]
        public int Value { get; set; }
    }
}
