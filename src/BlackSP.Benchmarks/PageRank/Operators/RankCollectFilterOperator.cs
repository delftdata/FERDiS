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
    public class RankCollectFilterOperator : IFilterOperator<PageEvent>
    {

        [ApplicationState]
        private readonly IDictionary<int, Models.Page> _pageRanks;

        public RankCollectFilterOperator()
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

            if (@event.Page.Epoch > Constants.MaxEpochCount)
            {
                return null; //dont continue further than the last epoch
            }

            var lastPage = _pageRanks[page.PageId];
            if(lastPage.Epoch == page.Epoch)
            {
                //lastPage.Rank += page.Rank;
                return new PageEvent {
                    Key = lastPage.PageId.ToString(),
                    Page = lastPage, 
                    EventTime = @event.EventTime
                };
            } 
            else if(lastPage.Epoch < page.Epoch)
            {
                _pageRanks[page.PageId] = page;
                return @event;
            }


            return null;
            //@event.Page.Epoch++;
            //return @event; //last page was in newer epoch than new page.. so we have it move to the next epoch and reiterate
        }
    }
}
