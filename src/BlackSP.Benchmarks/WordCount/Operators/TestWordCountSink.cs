using BlackSP.Checkpointing;
using BlackSP.Kernel.Operators;
using BlackSP.Benchmarks.WordCount.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using BlackSP.Benchmarks.Kafka;

namespace BlackSP.Benchmarks.WordCount.Operators
{
    class TestWordCountSink : ISinkOperator<WordEvent>
    {

        private readonly ILogger _logger;

        [ApplicationState]
        private IDictionary<string, int> _wordCountMap;

        public TestWordCountSink(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _wordCountMap = new Dictionary<string, int>();
        }

        public Task Sink(WordEvent @event)
        {
            if (_wordCountMap.ContainsKey(@event.Word))
            {
                _wordCountMap[@event.Word] += @event.Count;
            }
            else
            {
                _wordCountMap.Add(@event.Word, @event.Count);
            }
            var wordCountStrings = _wordCountMap.OrderBy(p => p.Key).Select(x => x.Key + "=" + x.Value).ToArray();
            _logger.Information($"WordCount: {string.Join("; ", wordCountStrings)}");

            return Task.CompletedTask;
        }
    }
}
