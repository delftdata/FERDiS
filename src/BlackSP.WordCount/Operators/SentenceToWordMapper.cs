using BlackSP.Kernel.Operators;
using BlackSP.WordCount.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.WordCount.Operators
{
    class SentenceToWordMapper : IMapOperator<SentenceEvent, WordEvent>
    {
        public IEnumerable<WordEvent> Map(SentenceEvent @event)
        {
            return @event.Sentence.Split(" ").Select(word => new WordEvent { EventTime = @event.EventTime, Word = word, Count = 1 });
        }
    }
}
