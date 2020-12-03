using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Models
{
    [ProtoContract]
    [Serializable]
    public class Auction
    {
        public static readonly string KafkaTopicName = "auctions";

        /// <summary>
        /// PK
        /// </summary>
        [ProtoMember(1)]
        public int Id { get; set; }

        /// <summary>
        /// FK
        /// </summary>
        [ProtoMember(2)]
        public int PersonId { get; set; }

        /// <summary>
        /// FK
        /// </summary>
        [ProtoMember(3)]
        public int ItemId { get; set; }

        /// <summary>
        /// FK
        /// </summary>
        [ProtoMember(4)]
        public int CategoryId { get; set; }

        [ProtoMember(5)]
        public int Quantity { get; set; }

        [ProtoMember(6)]
        public int StartTime { get; set; }

        [ProtoMember(7)]
        public int EndTime { get; set; }
    }
}
