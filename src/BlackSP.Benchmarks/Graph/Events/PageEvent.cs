using BlackSP.Benchmarks.Graph.Models;
using BlackSP.Core.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Kernel.Models;

namespace BlackSP.Benchmarks.Graph.Events
{

    [ProtoContract]
    [Serializable]
    public class PageEvent : IEvent
    {

        [ProtoMember(1)]
        public int? Key { get; set; }

        [ProtoMember(2)]
        public DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public Page Page { get; set; }
    }
}
