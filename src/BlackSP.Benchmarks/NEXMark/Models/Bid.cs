using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace BlackSP.Benchmarks.NEXMark.Models
{
    public class Bid
    {
        public static readonly string KafkaTopicName = "bids";


        /// <summary>
        /// 
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// FK
        /// </summary>
        public int PersonId { get; set; }

        /// <summary>
        /// FK
        /// </summary>
        public int AuctionId { get; set; }

        /// <summary>
        /// The amount of currency the bid is
        /// </summary>
        public double Amount { get; set; } 
    }
}
