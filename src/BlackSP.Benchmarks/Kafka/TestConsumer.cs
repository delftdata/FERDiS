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

        internal TestConsumer()
        {
            consumer = InitKafka();
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
            var brokerCount = int.Parse(Environment.GetEnvironmentVariable("KAFKA_BROKER_COUNT"));
            for (int i = 0; i < brokerCount; i++)
            {
                yield return new TopicPartitionOffset(new TopicPartition(topicName, i), Offset.Beginning);
            }
        }
    }
}
