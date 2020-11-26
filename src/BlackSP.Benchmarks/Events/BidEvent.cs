using BlackSP.Core.Models;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using BlackSP.Benchmarks.NEXMark.Models;

namespace BlackSP.Benchmarks.Events
{
    [ProtoContract]
    public class BidEvent : MD5PartitionKeyEventBase
    {
        [ProtoMember(1)]
        public override string Key { get; set; }

        [ProtoMember(2)]
        public override DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public Bid Bid { get; set; }
    }
}
