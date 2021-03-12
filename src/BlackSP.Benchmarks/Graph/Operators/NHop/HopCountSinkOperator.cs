using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.Graph.Operators
{
    public class HopCountSinkOperator : ISinkOperator<HopEvent>
    {

        private readonly ILogger _logger;

        private readonly IDictionary<string, int> _results;

        public HopCountSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _results = new Dictionary<string, int>();
        }

        public async Task Sink(HopEvent @event)
        {
            var neighbour = @event.Neighbour;
            var key = $"{neighbour.FromId}-{neighbour.ToId}";
            if(!_results.ContainsKey(key))
            {
                _results[key] = int.MaxValue;
            } 
            if(neighbour.Hops < _results[key])
            {
                _results[key] = neighbour.Hops;

                if(neighbour.Hops > 1) //to prevent logging trivial results
                {
                    _logger.Information($"NHop: [{neighbour.FromId:D4}, {neighbour.ToId:D4}] = {neighbour.Hops}");
                }
            }
        }
    }
}
