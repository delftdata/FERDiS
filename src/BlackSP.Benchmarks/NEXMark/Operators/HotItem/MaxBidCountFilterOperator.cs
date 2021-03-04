using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Checkpointing;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Operators.HotItem
{
    public class MaxBidCountFilterOperator : IFilterOperator<BidCountEvent>
    {
        /// <summary>
        /// Contains auctionId keys<br/>
        /// Contains bidCount values<br/>
        /// Note: is persisted
        /// </summary>
        [ApplicationState]
        private readonly IDictionary<int, int> _bidCounts;

        public MaxBidCountFilterOperator()
        {
            _bidCounts = new Dictionary<int, int>();
        }


        public BidCountEvent Filter(BidCountEvent @event)
        {
            UpdateCounts(@event.AuctionId, @event.Count);

            var maxValue = _bidCounts.Max(kv => kv.Value);
            if(_bidCounts.First(kv => kv.Value == maxValue).Key == @event.AuctionId)
            {
                return @event;
            }
            return null;
        }

        private void UpdateCounts(int auctionId, int bidCount)
        {
            if(!_bidCounts.ContainsKey(auctionId))
            {
                _bidCounts.Add(auctionId, 0);
            }
            _bidCounts[auctionId] = bidCount;
        }
    }
}
