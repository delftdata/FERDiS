using BlackSP.Kernel.Configuration;
using Confluent.Kafka;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.Kafka
{
    internal class TestConsumer
    {

        IConsumer<int, string> consumer;
        private IProducer<int, string> producer;

        internal TestConsumer()
        {
            consumer = InitKafka();

            var config = new ProducerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                Partitioner = Partitioner.Random,
                //LingerMs = 100
            };
            producer = new ProducerBuilder<int, string>(config).SetErrorHandler((prod, err) => Console.WriteLine($"Output produce error: {err}")).Build();
        }

        public void Consume()
        {
            var windowAt = DateTime.UtcNow;
            var c = 0;
            while (true)
            {
                var nextWindow = windowAt.AddMilliseconds(1000);
                var now = DateTime.UtcNow;

                if (nextWindow < now)
                {
                    Console.WriteLine($"resetting counter {c} to 0");
                    c = 0;
                    windowAt = nextWindow;
                }

                var res = consumer.Consume();
                if(res.Message.Value == "biep")
                {
                    Console.WriteLine("boop");
                }
                c++;

                var outputValue = $"{res.Message.Timestamp.UtcDateTime:yyyyMMddHHmmssFFFFF}${DateTime.UtcNow:yyyyMMddHHmmssFFFFF}$1";
                producer.Produce("output", new Message<int, string> { Key = res.Message.Value[0], Value = outputValue });
            }
        }


        private IConsumer<int, string> InitKafka()
        {
            var builder = new ConsumerBuilder<int, string>(new ConsumerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                GroupId = "lolmao",
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Latest
            }).SetErrorHandler((c, e) => { Console.WriteLine($"Kafka error: {e}"); });

            var consumer = builder.Build();
            var tpos = GetAssignedTopicPartitions("sentences");
            //assignedTopicPartitionOffsets = tpos;
            consumer.Assign(tpos);
            return consumer;
        }

        private IEnumerable<TopicPartitionOffset> GetAssignedTopicPartitions(string topicName)
        {
            var brokerCount = int.Parse(Environment.GetEnvironmentVariable("KAFKA_TOPIC_PARTITION_COUNT"));
            for (int i = 0; i < brokerCount; i++)
            {
                yield return new TopicPartitionOffset(new TopicPartition(topicName, i), Offset.Beginning);
            }
        }
    }
}
