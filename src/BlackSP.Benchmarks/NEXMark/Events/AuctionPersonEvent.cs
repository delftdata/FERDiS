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
    public class AuctionPersonEvent : IEvent
    {

        [ProtoMember(1)]
        public int? Key { get; set; }

        [ProtoMember(2)]
        public DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public Auction Auction { get; set; }

        [ProtoMember(4)]
        public Person Person { get; set; }
    }
}
