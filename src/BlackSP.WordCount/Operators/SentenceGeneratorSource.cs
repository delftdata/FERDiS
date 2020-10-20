using BlackSP.Checkpointing.Attributes;
using BlackSP.Kernel.Operators;
using BlackSP.WordCount.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.WordCount.Operators
{
    class SentenceGeneratorSource : ISourceOperator<SentenceEvent>
    {
        private static string[] defaultSentences = new[] { "A B C", "B C D", "C D A", "D A B" };
        
        [Checkpointable]
        private int lastSentenceIndex = 0;
        [Checkpointable]
        private int sentencesGenerated = 0;
        private readonly ILogger _logger;

        public SentenceGeneratorSource(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public SentenceEvent ProduceNext(CancellationToken t)
        {
            if(sentencesGenerated >= defaultSentences.Length * 20) //keep going until each sentence was sent 20 times
            {
                Task.Delay(WordCountAggregator.WindowSizeSeconds*2000).Wait();
                _logger.Information($"Each sentence sent at least 20 times, now sending all words as one sentence");
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
            _logger.Debug($"Sending {defaultSentences[i]} ({i} , {lastSentenceIndex})");
            return new SentenceEvent
            {
                EventTime = DateTime.UtcNow,
                Sentence = defaultSentences[i]
            };
        }
    }
}
