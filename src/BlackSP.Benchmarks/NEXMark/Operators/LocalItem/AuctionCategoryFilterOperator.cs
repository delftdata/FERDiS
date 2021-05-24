using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Operators.LocalItem
{
    class AuctionCategoryFilterOperator : IFilterOperator<AuctionEvent>
    {
        public AuctionEvent Filter(AuctionEvent @event)
        {
            int categoryId = @event.Auction.CategoryId;
            return categoryId % 2 !=0 ? @event : null;
        }
    }
}
