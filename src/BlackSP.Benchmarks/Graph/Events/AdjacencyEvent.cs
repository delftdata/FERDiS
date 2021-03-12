using BlackSP.Benchmarks.Graph.Models;
using BlackSP.Core.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.Graph.Events
{

    [ProtoContract]
    [Serializable]
    public class AdjacencyEvent : MD5PartitionKeyEventBase
    {

        [ProtoMember(1)]
        public override string Key { get; set; }

        [ProtoMember(2)]
        public override DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public Adjacency Adjacancy { get; set; }
    }
}
