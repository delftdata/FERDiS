using BlackSP.Kernel.Operators;
using BlackSP.WordCount.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BlackSP.WordCount.Operators
{
    class SentenceGeneratorSource : ISourceOperator<SentenceEvent>
    {
        private static string defaultSentence = "A B C D E F G H I J K L M N O P Q R S T U V W X Y Z";

        public SentenceEvent ProduceNext(CancellationToken t)
        {
            return new SentenceEvent
            {
                Key = defaultSentence,
                EventTime = DateTime.UtcNow,
                Sentence = defaultSentence
            };
        }
    }
}
