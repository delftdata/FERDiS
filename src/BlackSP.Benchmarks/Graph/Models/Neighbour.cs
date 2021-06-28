using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.Graph.Models
{
    /// <summary>
    /// Model for NHop
    /// </summary>
    [ProtoContract]
    [Serializable]
    public class Neighbour
    {

        public static readonly string KafkaTopicName = "neighbours";


        /// <summary>
        /// Beginning of neighbour relation
        /// </summary>
        [ProtoMember(1)]
        public int FromId { get; set; }

        /// <summary>
        /// End of neighbour relation
        /// </summary>
        [ProtoMember(2)]
        public int ToId { get; set; }

        /// <summary>
        /// Number of hops between FromId and ToId
        /// </summary>
        [ProtoMember(3)]
        public int Hops { get; set; }

    }
}
