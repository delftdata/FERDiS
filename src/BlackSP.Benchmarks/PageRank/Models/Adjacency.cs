using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.PageRank.Models
{

    /// <summary>
    /// Model class containing a page and its neighbours
    /// </summary>
    [ProtoContract]
    [Serializable]
    public class Adjacency
    {

        public static readonly string KafkaTopicName = "adjacency";

        [ProtoMember(1)]
        public int PageId { get; set; }

        /// <summary>
        /// Contains PageIds of neighbouring pages
        /// </summary>
        [ProtoMember(2)]
        public int[] Neighbours { get; set; } 

    }
}
