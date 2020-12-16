using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.PageRank.Models
{

    /// <summary>
    /// Model containing a page and its rank
    /// </summary>
    [ProtoContract]
    [Serializable]
    public class Page
    {
        [ProtoMember(1)]
        public int PageId { get; set; }

        [ProtoMember(2)]
        public double Rank { get; set; }

        [ProtoMember(3)]
        public int Epoch { get; set; }
    }
}
