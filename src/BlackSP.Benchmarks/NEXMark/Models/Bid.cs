using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace BlackSP.Benchmarks.NEXMark.Models
{
    [ProtoContract]
    public class Bid
    {
        public static readonly string KafkaTopicName = "bids";


        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(1)]
        public int Time { get; set; }

        /// <summary>
        /// FK
        /// </summary>
        [ProtoMember(2)]
        public int PersonId { get; set; }

        /// <summary>
        /// FK
        /// </summary>
        [ProtoMember(3)]
        public int AuctionId { get; set; }

        /// <summary>
        /// The amount of currency the bid is
        /// </summary>
        [ProtoMember(4)]
        public double Amount { get; set; } 
    }
}
