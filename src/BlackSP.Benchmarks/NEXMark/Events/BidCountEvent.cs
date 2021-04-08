using BlackSP.Core.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Kernel.Models;

namespace BlackSP.Benchmarks.NEXMark.Events
{
    [ProtoContract]
    [Serializable]
    public class BidCountEvent : IEvent
    {
        [ProtoMember(1)]
        public int? Key { get; set; }

        [ProtoMember(2)]
        public DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public int Count { get; set; }

        [ProtoMember(4)]
        public int AuctionId { get; set; }

        public int EventCount() => Count;
    }
}
