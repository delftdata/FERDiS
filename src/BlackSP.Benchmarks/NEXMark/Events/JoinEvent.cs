using BlackSP.Benchmarks.NEXMark.Models;
using BlackSP.Core.Models;
using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Events
{
    [ProtoContract]
    [Serializable]
    public class JoinEvent : MD5PartitionKeyEventBase
    {

        [ProtoMember(1)]
        public override string Key { get; set; }

        [ProtoMember(2)]
        public override DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public Auction EventA { get; set; }

        [ProtoMember(4)]
        public Person EventB { get; set; }
    }
}
