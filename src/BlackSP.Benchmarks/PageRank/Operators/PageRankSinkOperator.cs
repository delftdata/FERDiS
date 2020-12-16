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
            _logger.Information($"PageRank {page.PageId:N8} : {page.Rank:N5}");
        }
    }
}
