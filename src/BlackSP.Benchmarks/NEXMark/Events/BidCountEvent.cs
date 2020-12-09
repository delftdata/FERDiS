using BlackSP.Core.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Events
{
    [ProtoContract]
    [Serializable]
    public class BidCountEvent : MD5PartitionKeyEventBase
    {
        [ProtoMember(1)]
        public override string Key { get; set; }

        [ProtoMember(2)]
        public override DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public int Count { get; set; }

        [ProtoMember(4)]
        public int AuctionId { get; set; }
    }
}
