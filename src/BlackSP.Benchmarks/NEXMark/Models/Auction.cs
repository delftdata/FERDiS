using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Models
{
    public class Auction
    {
        public static readonly string KafkaTopicName = "auctions";


        public int Id { get; set; }
        public int PersonId { get; set; }
        public int ItemId { get; set; }
        public int CategoryId { get; set; }
        public int Quantity { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
    }
}
