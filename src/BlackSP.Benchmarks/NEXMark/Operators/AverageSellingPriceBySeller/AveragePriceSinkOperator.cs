using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.NEXMark.Operators.AverageSellingPriceBySeller
{
    public class AveragePriceSinkOperator : ISinkOperator<AveragePricePersonEvent>
    {

        private readonly ILogger _logger;

        public AveragePriceSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task Sink(AveragePricePersonEvent @event)
        {
            _logger.Information($"Person {@event.PersonId:0000} avg price {@event.AverageSellingPrice:N2}");
            return Task.CompletedTask;
        }
    }
}
