using BlackSP.Benchmarks.Kafka;
using BlackSP.Kernel.Configuration;
using Confluent.Kafka;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Logging;
using Serilog.Events;
using System.Globalization;

namespace BlackSP.Benchmarks.MetricCollection
{

    /// <summary>
    /// Consumes the output topic (containing input & output timestamps)
    /// </summary>
    public class E2ELatencyCalculatingConsumer
    {

        private readonly IConsumer<int, string> consumer;
        private readonly ILogger latencyLogger;
        private readonly ILogger errorLogger;
        
        public E2ELatencyCalculatingConsumer()
        {
            consumer = InitKafka();
            var targets = (LogTargetFlags)int.Parse(Environment.GetEnvironmentVariable("LOG_TARGET_FLAGS"));
            var level = (LogEventLevel)int.Parse(Environment.GetEnvironmentVariable("LOG_EVENT_LEVEL"));
            latencyLogger = new LoggerConfiguration().ConfigureMetricSinks(targets, level, "latency", "performance").CreateLogger();
            latencyLogger.Information("timestamp, latency_ms");

            errorLogger = new LoggerConfiguration().ConfigureSinks(targets, level, "latency-logger").CreateLogger();

        }

        public void Start()
        {
            while(true)
            {
                var consumeRes = consumer.Consume(); 
                //consumer.Seek(new TopicPartitionOffset(null, Offset.End))
                var output = consumeRes.Message.Value.Split("$");
                var inputTime = DateTime.ParseExact(output[0], "yyyyMMddHHmmssFFFFF", null, DateTimeStyles.None);
                var outputTime = consumeRes.Message.Timestamp.UtcDateTime; // DateTime.ParseExact(output[1], "yyyyMMddHHmmssFFFFF", null, DateTimeStyles.None);
                var latency = outputTime - inputTime;
                latencyLogger.Information($"{outputTime:hh:mm:ss:ffffff}, {(int)latency.TotalMilliseconds}");
            }
        }

        private IConsumer<int, string> InitKafka()
        {
            var builder = new ConsumerBuilder<int, string>(new ConsumerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                GroupId = "e2e-latency",
                EnableAutoCommit = true,
                AutoOffsetReset = AutoOffsetReset.Latest
            }).SetErrorHandler((c,e) => errorLogger.Warning($"Kafka error: {e}"));

            var consumer = builder.Build();
            var topicPartitions = GetAssignedTopicPartitions("output");
            consumer.Assign(topicPartitions);
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
