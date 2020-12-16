using BlackSP.Benchmarks.PageRank.Events;
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

        public PageRankSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Sink(PageEvent @event)
        {
            var page = @event.Page;
            if(page.Rank > 1e-7) //top ranks expected in (~0.001, ~0.02) range
            {
                _logger.Information($"PageRank {page.PageId:D7} : {page.Rank:E3}");
            }
        }
    }
}
