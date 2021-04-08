using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Kernel.Models;
using ProtoBuf;

namespace BlackSP.Serialization.UnitTests.Utilities
{
    [ProtoContract]
    public class ProtoBufTestEvent : IEvent
    {
        [ProtoMember(1)]
        public int? Key { get; set; }

        [ProtoMember(2)]
        public DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public int Value { get; set; }

        public ProtoBufTestEvent()
        {

        }

        public ProtoBufTestEvent(int? key, DateTime? eventTime, int value)
        {
            Key = key;
            EventTime = eventTime ?? throw new ArgumentNullException(nameof(eventTime));
            Value = value;
        }

        public int GetPartitionKey()
        {
            return Key.GetHashCode();
        }
    }
}
