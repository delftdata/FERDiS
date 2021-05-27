using BlackSP.Benchmarks.Kafka;
using BlackSP.Benchmarks.WordCount.Events;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using Confluent.Kafka;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BlackSP.Benchmarks.WordCount.Operators
{
    public class KafkaSentenceSource : Kafka.KafkaSourceConsumerBase<string>, ISourceOperator<SentenceEvent>
    {
        protected override string TopicName => "sentences";

        private IProducer<int, string> producer;


        public KafkaSentenceSource(IVertexConfiguration vertexConfig, ILogger logger): base(vertexConfig, logger)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                Partitioner = Partitioner.Consistent
            };
            producer = new ProducerBuilder<int, string>(config).SetErrorHandler((prod, err) => logger.Warning($"Output produce error: {err}")).Build();
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

            //var outputValue = $"{sentenceEvent.EventTime:yyyyMMddHHmmssFFFFF}${DateTime.UtcNow:yyyyMMddHHmmssFFFFF}${((IEvent)sentenceEvent).EventCount()}";
            //producer.ProduceAsync("output", new Message<int, string> { Key = sentenceEvent.Key ?? default, Value = outputValue });

            UpdateOffsets(consumeRes.Partition, consumeRes.Offset);
            return sentenceEvent;
        }
    }
}
