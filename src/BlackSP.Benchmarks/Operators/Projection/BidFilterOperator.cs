using BlackSP.Benchmarks.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.Operators.Projection
{
    public class BidFilterOperator : IFilterOperator<BidEvent>
    {
        public BidEvent Filter(BidEvent @event)
        {
            if(@event.Bid.AuctionId > 20 || @event.Bid.AuctionId < 40)
            {
                return @event;
            }
            return null;
        }
    }
}
