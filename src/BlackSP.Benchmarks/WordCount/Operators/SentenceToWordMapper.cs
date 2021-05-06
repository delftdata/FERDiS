using BlackSP.Kernel.Operators;
using BlackSP.Benchmarks.WordCount.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlackSP.Kernel.Configuration;

namespace BlackSP.Benchmarks.WordCount.Operators
{
    class SentenceToWordMapper : IMapOperator<SentenceEvent, WordEvent>
    {

        public IEnumerable<WordEvent> Map(SentenceEvent @event)
        {
            return @event.Sentence.Replace(',', ' ').Replace('.', ' ').Split(" ").Select(word => new WordEvent { EventTime = @event.EventTime, Word = word, Count = 1 });
        }
    }
}
