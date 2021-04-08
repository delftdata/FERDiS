using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Operators.HotItem
{
    public class BidCountAggregateOperator : IAggregateOperator<BidEvent, BidCountEvent>
    {
        public TimeSpan WindowSize => TimeSpan.FromSeconds(10);

        public TimeSpan WindowSlideSize => TimeSpan.FromSeconds(1);

        public IEnumerable<BidCountEvent> Aggregate(IEnumerable<BidEvent> window)
        {
            return window
                .GroupBy(b => b.Bid.AuctionId)
                .Select(group => new BidCountEvent
                {
                    Key = group.Key,
                    AuctionId = group.Key,
                    Count = group.Count()
                });
        }
    }
}
