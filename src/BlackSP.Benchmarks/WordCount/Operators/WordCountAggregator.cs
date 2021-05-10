using BlackSP.Kernel.Operators;
using BlackSP.Benchmarks.WordCount.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Benchmarks.WordCount.Operators
{
    public class WordCountAggregator : IAggregateOperator<WordEvent, WordEvent>
    {
        public TimeSpan WindowSize => TimeSpan.FromMilliseconds(Constants.WordCountAggregateWindowSizeMs);
        public TimeSpan WindowSlideSize => TimeSpan.FromMilliseconds(Constants.WordCountAggregateWindowSizeMs);

        private readonly ILogger _logger;

        public WordCountAggregator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<WordEvent> Aggregate(IEnumerable<WordEvent> window)
        {
            var wordGroups = window.GroupBy(ev => ev.Word);
            _logger.Debug($"Aggregating {wordGroups.Count()} different words");
            foreach (var group in wordGroups)
            {
                var count = group.Sum(ev => ev.Count);
                if(count == 0) { continue; }

                yield return new WordEvent
                {
                    EventTime = group.Max(e => e.EventTime),
                    Word = group.Key,
                    Count = group.Sum(ev => ev.Count),
                    EC = group.Count()
                };
            }
        }
    }
}
