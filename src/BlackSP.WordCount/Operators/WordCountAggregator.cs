using BlackSP.Kernel.Operators;
using BlackSP.WordCount.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.WordCount.Operators
{
    public class WordCountAggregator : IAggregateOperator<WordEvent, WordEvent>
    {
        public TimeSpan WindowSize => TimeSpan.FromSeconds(1);

        public IEnumerable<WordEvent> Aggregate(IEnumerable<WordEvent> window)
        {
            foreach(var group in window.GroupBy(ev => ev.Word))
            {
                var word = group.Key;
                var count = group.Sum(ev => ev.Count);
                yield return new WordEvent
                {
                    EventTime = group.First().EventTime,
                    Word = word,
                    Count = count
                };
            }
        }
    }
}
