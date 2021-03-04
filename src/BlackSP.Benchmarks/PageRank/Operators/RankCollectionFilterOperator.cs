using BlackSP.Benchmarks.PageRank.Events;
using BlackSP.Checkpointing;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.PageRank.Operators
{

    /// <summary>
    /// Collects pageranks and when unchanged filters them
    /// </summary>
    public class RankCollectionFilterOperator : IFilterOperator<PageEvent>
    {

        [ApplicationState]
        private readonly IDictionary<int, Models.Page> _pageRanks;

        public RankCollectionFilterOperator()
        {
            _pageRanks = new Dictionary<int, Models.Page>();
        }

        public PageEvent Filter(PageEvent @event)
        {
            var page = @event.Page;
            if(!_pageRanks.ContainsKey(page.PageId))
            {
                _pageRanks.Add(page.PageId, page);
                return @event;
            }
            //if epoch == then +
            //if epoch < then ignore
            //if epoch > then overwrite
            var lastPage = _pageRanks[page.PageId];
            _pageRanks[page.PageId] = page;
            return page.Rank != lastPage.Rank ? @event : null;      
        }
    }
}
