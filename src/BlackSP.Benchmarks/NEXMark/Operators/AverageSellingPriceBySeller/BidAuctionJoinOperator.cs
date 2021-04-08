using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Operators.AverageSellingPriceBySeller
{
    /// <summary>
    /// Only joins bids that were have been placed during the auction time (none too early or late)
    /// </summary>
    public class BidAuctionJoinOperator : IJoinOperator<BidEvent, AuctionEvent, BidAuctionEvent>
    {
        //Note: "unbounded" window

        public TimeSpan WindowSize => TimeSpan.FromDays(365);

        public TimeSpan WindowSlideSize => TimeSpan.FromDays(365);

        public BidAuctionEvent Join(BidEvent matchA, AuctionEvent matchB)
        {
            return new BidAuctionEvent
            {
                Key = matchB.Auction.Id,
                Bid = matchA.Bid,
                Auction = matchB.Auction
            };
        }

        public bool Match(BidEvent testA, AuctionEvent testB)
        {
            var bidTime = testA.Bid.Time;
            var auctionStart = testB.Auction.StartTime;
            var auctionEnd = testB.Auction.EndTime;
            return testA.Bid.AuctionId == testB.Auction.Id 
                && bidTime > auctionStart 
                && bidTime < auctionEnd;
        }
    }
}
