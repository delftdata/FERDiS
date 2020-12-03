using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.NEXMark.Operators.Projection
{
    class BidSinkOperator : ISinkOperator<BidEvent>
    {

        private readonly ILogger _logger;

        public BidSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task Sink(BidEvent @event)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var bid = @event.Bid;
            _logger.Information($"pid:{bid.PersonId},aid{bid.AuctionId},amt:{bid.Amount}");
        }
    }
}
