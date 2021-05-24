using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Operators.Projection
{
    public class BidFilterOperator : IFilterOperator<BidEvent>
    {
        public BidEvent Filter(BidEvent @event)
        {
            if(@event.Bid.AuctionId % 2 != 0) //filter out uneven auctionIds
            {
                return @event;
            }
            return null;
        }
    }
}
