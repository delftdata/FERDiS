using BlackSP.Benchmarks.PageRank.Events;
using BlackSP.Benchmarks.PageRank.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Benchmarks.PageRank.Operators
{
    public class RankSumAggregateOperator : IAggregateOperator<PageEvent, PageEvent>
    {
        public TimeSpan WindowSize => Constants.RankSumWindowSize;

        public TimeSpan WindowSlideSize => Constants.RankSumWindowSlideSize;

        public IEnumerable<PageEvent> Aggregate(IEnumerable<PageEvent> window)
        {
            return window.Select(pe => pe.Page)
                         .GroupBy(p => (PageId: p.PageId, Epoch: p.Epoch))
                         .Select(group => new Page
                         {
                             PageId = group.Key.PageId,
                             Rank = group.Sum(p => p.Rank),
                             Epoch = group.Key.Epoch
                         }) 
                         .Select(p => new PageEvent
                         {
                             Key = p.PageId.ToString(),
                             Page = p
                         });
        }
    }
}
