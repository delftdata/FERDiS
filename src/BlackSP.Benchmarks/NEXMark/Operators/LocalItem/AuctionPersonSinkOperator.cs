using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Benchmarks.NEXMark.Models;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.NEXMark.Operators.LocalItem
{
    public class AuctionPersonSinkOperator : ISinkOperator<JoinEvent>
    {

        private readonly ILogger _logger;

        public AuctionPersonSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Sink(JoinEvent @event)
        {
            var auction = @event.EventA;
            var person = @event.EventB;
            _logger.Information($"[ {person.FullName}, {person.Address.Street}, {person.Address.Zipcode}, {person.Address.Province}, {auction.CategoryId} ]");
        }
    }
}
