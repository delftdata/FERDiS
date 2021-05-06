using BlackSP.Benchmarks.WordCount.Events;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BlackSP.Benchmarks.WordCount.Operators
{
    public class KafkaSentenceSource : Kafka.KafkaConsumerBase<string>, ISourceOperator<SentenceEvent>
    {
        protected override string TopicName => "sentences";

        public KafkaSentenceSource(IVertexConfiguration vertexConfig, ILogger logger): base(vertexConfig, logger)
        {

        }

        public SentenceEvent ProduceNext(CancellationToken t)
        {
            var consumeRes = Consumer.Consume(t) ?? throw new Exception("Kafka consume returned null result");
            var msg = consumeRes.Message;
            var sentenceEvent = new SentenceEvent
            {
                Sentence = msg.Value,
                EventTime = msg.Timestamp.UtcDateTime
            };

            return sentenceEvent;
        }
    }
}
