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
        public TimeSpan WindowSize => TimeSpan.FromSeconds(15);
        public TimeSpan WindowSlideSize => TimeSpan.FromSeconds(5);

        public IEnumerable<AuctionSellingPriceEvent> Aggregate(IEnumerable<BidAuctionEvent> window)
        {
            return window
                .GroupBy(ev => ev.Auction.Id)
                .Select(gr => (Group: gr, MaxBid: gr.Max(x => x.Bid.Amount)))
                .Select(p => (Group: p.Group, MaxBidAuction: p.Group.FirstOrDefault(ev => ev.Bid.Amount == p.MaxBid)))
                .Select(p => new AuctionSellingPriceEvent
                {
                    Key = p.MaxBidAuction.Auction.PersonId, //Partition by person
                    Auction = p.MaxBidAuction.Auction,
                    SellingPrice = p.MaxBidAuction.Bid.Amount,
                    EventTime = p.Group.Max(e => e.EventTime)
                });
        }
    }
}
