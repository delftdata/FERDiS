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
        private static string[] defaultSentences = new[] { "A", "A B", "A B C", "A B C D" };
        
        [Checkpointable]
        private int lastSentenceIndex;
        
        [Checkpointable]
        private int sentencesGenerated;
        
        private readonly ILogger _logger;

        public SentenceGeneratorSource(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            lastSentenceIndex = 0;
            sentencesGenerated = 0;
        }

        public SentenceEvent ProduceNext(CancellationToken t)
        {
            if(sentencesGenerated >= defaultSentences.Length * 500000) //keep going until each sentence was sent 20 times
            {
                Task.Delay(WordCountAggregator.WindowSizeSeconds*2000).Wait();
                _logger.Information($"Each sentence sent at least 500.000 times, now sending all words as one sentence");
                return new SentenceEvent
                {
                    EventTime = DateTime.UtcNow,
                    Sentence = string.Join(" ", defaultSentences)
                };
            } 
            else
            {
                //Task.Delay(1000).Wait();//nasty sleep to throttle the amount of data being generated
            }
            var i = lastSentenceIndex;
            lastSentenceIndex = (lastSentenceIndex + 1) % defaultSentences.Length; //round robin pick sentences
            sentencesGenerated++;
            //_logger.Debug($"Sending {defaultSentences[i]} ({i} , {lastSentenceIndex})");
            return new SentenceEvent
            {
                EventTime = DateTime.UtcNow,
                Sentence = defaultSentences[i]
            };
        }
    }
}
