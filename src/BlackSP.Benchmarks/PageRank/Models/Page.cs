using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.PageRank.Models
{

    [ProtoContract]
    public class Page
    {
        [ProtoMember(1)]
        public int PageId { get; set; }

        [ProtoMember(2)]
        public double Rank { get; set; }
    }
}
