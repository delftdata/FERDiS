using BlackSP.Benchmarks.PageRank.Events;
using BlackSP.Benchmarks.PageRank.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Benchmarks.PageRank.Operators
{
    public class PageRankSumAggregateOperator : IAggregateOperator<PageEvent, PageEvent>
    {
        public TimeSpan WindowSize => TimeSpan.FromSeconds(10);

        public TimeSpan WindowSlideSize => TimeSpan.FromSeconds(10);

        public IEnumerable<PageEvent> Aggregate(IEnumerable<PageEvent> window)
        {
            return window.Select(pe => pe.Page)
                         .GroupBy(p => p.PageId)
                         .Select(group => new Page
                         {
                             PageId = group.Key,
                             Rank = group.Sum(p => p.Rank)
                         })
                         .Select(p => new PageEvent
                         {
                             Key = p.PageId.ToString(),
                             Page = p
                         });
        }
    }
}
