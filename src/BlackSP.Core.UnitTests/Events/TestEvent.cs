using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.UnitTests.Events
{
    [ProtoContract]
    public class TestEvent : IEvent {
        
        [ProtoMember(1)]
        public byte Value { get; set; }
        
        [ProtoMember(2)]
        public string Key { get; set; }
        
        [ProtoMember(3)]
        public DateTime EventTime { get; set; }

        public int GetPartitionKey()
        {
            return Key.GetHashCode();
        }
    }
}
