using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Benchmarks.Graph.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.Graph.Operators
{
    public class InitialRankMapOperator : IMapOperator<AdjacencyEvent, PageEvent>
    {

        /// <summary>
        /// Representing a count as a double to not repeatedly cast to double during division in Map method
        /// </summary>
        private readonly double TotalPageCount;

        public InitialRankMapOperator()
        {
            string pageCountString = Environment.GetEnvironmentVariable("PR_PAGE_COUNT") ?? throw new InvalidOperationException("Missing environment variable PR_PAGE_COUNT");
            TotalPageCount = double.Parse(pageCountString);
        }

        public IEnumerable<PageEvent> Map(AdjacencyEvent @event)
        {
            var page = new Page
            {
                PageId = @event.Adjacancy.PageId,
                Rank = 1 / TotalPageCount,
                Epoch = 0
            };

            yield return new PageEvent
            {
                Key = page.PageId,
                Page = page
            };
        }
    }
}
