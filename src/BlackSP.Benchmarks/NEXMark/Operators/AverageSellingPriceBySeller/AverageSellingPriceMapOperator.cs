using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Checkpointing;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Operators.AverageSellingPriceBySeller
{
    class AverageSellingPriceMapOperator : IMapOperator<AuctionSellingPriceEvent, AveragePricePersonEvent>
    {

        public AverageSellingPriceMapOperator()
        {
            state = new Dictionary<int, (double, int)>();
        }

        /// <summary>
        /// Key personId
        /// Value (total selling price, auction count)
        /// </summary>
        [ApplicationState]
        private readonly IDictionary<int, (double, int)> state;

        public IEnumerable<AveragePricePersonEvent> Map(AuctionSellingPriceEvent @event)
        {
            var key = @event.Auction.PersonId;
            if(!state.ContainsKey(key))
            {
                state.Add(key, (0d, 0));
            }

            //update local state..
            var (total, count) = state[key];
            total += @event.SellingPrice;
            count += 1;
            state[key] = (total, count);

            //yield new average
            yield return new AveragePricePersonEvent
            {
                Key = @event.Auction.PersonId,
                PersonId = @event.Auction.PersonId,
                AverageSellingPrice = total/count,
                Count = 1,
                EventTime = @event.EventTime
            };
        }
    }
}
