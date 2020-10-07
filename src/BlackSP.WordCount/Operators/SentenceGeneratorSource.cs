using BlackSP.Checkpointing.Attributes;
using BlackSP.Kernel.Operators;
using BlackSP.WordCount.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.WordCount.Operators
{
    class SentenceGeneratorSource : ISourceOperator<SentenceEvent>
    {
        private static string[] defaultSentences = new[] { "A B", "C D", "E F", "G H" };
        
        [Checkpointable]
        private int lastSentenceIndex = 0;

        [Checkpointable]
        private int sentencesGenerated = 0;
        public SentenceEvent ProduceNext(CancellationToken t)
        {
            if(sentencesGenerated >= 40)
            {
                Task.Delay(WordCountAggregator.WindowSizeSeconds*2000).Wait(); //
                return new SentenceEvent
                {
                    EventTime = DateTime.UtcNow,
                    Sentence = string.Join(" ", defaultSentences)
                };
            } 
            else
            {
                Task.Delay(1000).Wait();//nasty sleep to throttle the amount of data being generated
            }
            var i = lastSentenceIndex;
            lastSentenceIndex = (lastSentenceIndex + 1) % defaultSentences.Length; //round robin pick sentences
            sentencesGenerated++;
            return new SentenceEvent
            {
                EventTime = DateTime.UtcNow,
                Sentence = defaultSentences[i]
            };
        }
    }
}
