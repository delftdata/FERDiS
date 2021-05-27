using BlackSP.Checkpointing;
using BlackSP.Kernel.Operators;
using BlackSP.Benchmarks.WordCount.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Kernel.Configuration;

namespace BlackSP.Benchmarks.WordCount.Operators
{
    class TestSentenceGeneratorSource : Kafka.KafkaSourceConsumerBase<string>, ISourceOperator<SentenceEvent>
    {
        private static string[] defaultSentences = new[] { "asdlkfnmjalksjnhfdlkashbfkjasdbhf", "asldkfjnmalskfnhjlakshdfbnkjashdf asdflkmjnaskldfjlkashdfasdf", "asdflkmjaslkfdjasklhdjfasdf asdflknaskldfjnalskdhjfnasdfasdfasf askldjfnaslkjdfhnaslkhjdfasdfasdf", "asdflkmnjcasklhjfdnasdf askldfjmhalkjhcnrasdf lkjfdmlakjsdflaksjhnfdlkasd lsdjkmgfkasjhdfnakjha" };//new[] { "A", "A B", "A B C", "A B C D" };

        [ApplicationState]
        private int lastSentenceIndex;
        
        [ApplicationState]
        private int sentencesGenerated;
        
        private readonly ILogger _logger;

        private int MaxSentenceCount => int.MaxValue;// defaultSentences.Length * 50000;

        protected override string TopicName => "sentences";

        public TestSentenceGeneratorSource(IVertexConfiguration config, ILogger logger) : base(config, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            lastSentenceIndex = 0;
            sentencesGenerated = 0;
        }

        public SentenceEvent ProduceNext(CancellationToken t)
        {
            if(sentencesGenerated >= MaxSentenceCount) //keep going until each sentence was sent x times
            {
                Task.Delay(Constants.WordCountAggregateWindowSizeMs*2).Wait();
                //_logger.Information($"Each sentence sent at least {MaxSentenceCount} times, now sending all words as one sentence");
                return new SentenceEvent
                {
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
            //var x = Consumer.Consume(t);
            //var huh = x.Message.Value;
            //if (huh == "lol")
            //{
            //    sentencesGenerated++;
            //}
            return new SentenceEvent
            {
                Sentence = defaultSentences[i],
                EventTime = DateTime.UtcNow
            };
        }
    }
}
