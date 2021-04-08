using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Kernel.Models;
using ProtoBuf;
namespace BlackSP.Serialization.UnitTests.Utilities
{
    [ProtoContract]
    public class ProtoBufTestEvent2 : IEvent
    {
        [ProtoMember(1)]
        public int? Key { get; set; }

        [ProtoMember(2)]
        public DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public string Value { get; set; }

        public ProtoBufTestEvent2() : base()
        {

        }

        public ProtoBufTestEvent2(int key, DateTime? eventTime, string value2)
        {
            Key = key;
            EventTime = eventTime ?? throw new ArgumentNullException(nameof(eventTime));
            Value = value2;
        }

        public int GetPartitionKey()
        {
            return Key.GetHashCode();
        }
    }
}
