using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.NEXMark.Operators.HotItem
{
    public class BidCountSinkOperator : ISinkOperator<BidCountEvent>
    {

        private readonly ILogger _logger;

        public BidCountSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Sink(BidCountEvent @event)
        {
            _logger.Information($"Hot Item: {@event.AuctionId} ({@event.Count})");
        }
    }
}
