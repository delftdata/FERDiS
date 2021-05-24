using BlackSP.Core.Models;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using BlackSP.Benchmarks.NEXMark.Models;

namespace BlackSP.Benchmarks.NEXMark.Events
{
    [ProtoContract]
    [Serializable]
    public class BidEvent : IEvent
    {
        [ProtoMember(1)]
        public int? Key { get; set; }

        [ProtoMember(2)]
        public DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public Bid Bid { get; set; }

    }
}
