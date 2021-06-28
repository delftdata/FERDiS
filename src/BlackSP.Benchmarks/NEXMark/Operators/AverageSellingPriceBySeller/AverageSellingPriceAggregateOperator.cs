using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Operators.AverageSellingPriceBySeller
{
    class AverageSellingPriceAggregateOperator : IAggregateOperator<AuctionSellingPriceEvent, AveragePricePersonEvent>
    {
        public TimeSpan WindowSize => TimeSpan.FromSeconds(5);

        public TimeSpan WindowSlideSize => TimeSpan.FromSeconds(5);

        public IEnumerable<AveragePricePersonEvent> Aggregate(IEnumerable<AuctionSellingPriceEvent> window)
        {
            var personGroups = window.GroupBy(ev => ev.Auction.PersonId);
            return personGroups.Select(gr => new AveragePricePersonEvent
            {
                Key = gr.Key,
                PersonId = gr.Key,
                AverageSellingPrice = gr.Average(ev => ev.SellingPrice),
                Count = gr.Count(),
                EventTime = gr.Max(x => x.EventTime)
            });

        }
    }
}
