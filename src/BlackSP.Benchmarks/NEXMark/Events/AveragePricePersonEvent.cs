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
    public class AveragePricePersonEvent : IEvent
    {
        [ProtoMember(1)]
        public int? Key { get; set; }

        [ProtoMember(2)]
        public DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public int PersonId { get; set; }

        [ProtoMember(4)]
        public double AverageSellingPrice { get; set; }

        [ProtoMember(5)]
        public int Count { get; set; }

        public int EventCount() => Count;
    }
}
