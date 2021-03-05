using BlackSP.Benchmarks.PageRank.Events;
using BlackSP.Checkpointing;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.PageRank.Operators
{
    public class PageRankSinkOperator : ISinkOperator<PageEvent>
    {

        private readonly ILogger _logger;

        [ApplicationState]
        private readonly IDictionary<int, Models.Page> _pageRanks;


        public PageRankSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pageRanks = new Dictionary<int, Models.Page>();
        }

        public async Task Sink(PageEvent @event)
        {
            var newPage = @event.Page;
            bool rankDidChange = false;
            
            if(_pageRanks.TryGetValue(newPage.PageId, out var existingPage))
            {
                _pageRanks.Remove(newPage.PageId);
                if(existingPage.Epoch == newPage.Epoch) //updated rank arrived
                {
                    rankDidChange = Math.Abs(newPage.Rank - existingPage.Rank) > 0.0001d;
                } 
                else if(existingPage.Epoch < newPage.Epoch) //new epoch arrived
                {
                    rankDidChange = true;
                }
                else
                {
                    newPage = existingPage;
                }
            }

            _pageRanks.Add(newPage.PageId, newPage);

            if (newPage.Epoch % Constants.EpochSinkInterval == 0 && rankDidChange)
            {
                _logger.Information($"PageRank {newPage.PageId:D7} : {newPage.Rank:E3} : {newPage.Epoch:D2}");
            }
        }
    }
}
