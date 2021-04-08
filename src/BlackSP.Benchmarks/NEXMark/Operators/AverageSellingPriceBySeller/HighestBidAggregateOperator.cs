using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Operators.AverageSellingPriceBySeller
{
    public class HighestBidAggregateOperator : IAggregateOperator<BidAuctionEvent, AuctionSellingPriceEvent>
    {
        public TimeSpan WindowSize => TimeSpan.FromSeconds(10);
        public TimeSpan WindowSlideSize => TimeSpan.FromSeconds(10);

        public IEnumerable<AuctionSellingPriceEvent> Aggregate(IEnumerable<BidAuctionEvent> window)
        {
            return window
                .GroupBy(ev => ev.Auction.Id)
                .Select(gr => (Group: gr, MaxBid: gr.Max(x => x.Bid.Amount)))
                .Select(p => p.Group.FirstOrDefault(ev => ev.Bid.Amount == p.MaxBid))
                .Select(ev => new AuctionSellingPriceEvent
                {
                    Key = ev.Auction.PersonId, //Partition by person
                    Auction = ev.Auction,
                    SellingPrice = ev.Bid.Amount
                });
        }
    }
}
