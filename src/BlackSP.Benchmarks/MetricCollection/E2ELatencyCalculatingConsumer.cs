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
using System.Threading.Tasks;
using System.Threading;

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

        private IEnumerable<TopicPartitionOffset> assignedTopicPartitionOffsets;

        public E2ELatencyCalculatingConsumer()
        {
            consumer = InitKafka();
            var targets = (LogTargetFlags)int.Parse(Environment.GetEnvironmentVariable("LOG_TARGET_FLAGS"));
            var level = (LogEventLevel)int.Parse(Environment.GetEnvironmentVariable("LOG_EVENT_LEVEL"));
            latencyLogger = new LoggerConfiguration().ConfigureMetricSinks(targets, level, "latency", "performance").CreateLogger();
            latencyLogger.Information("timestamp, latency_ms, shardId");
            latencyLogger.Information($"{DateTime.UtcNow:hh:mm:ss:ffffff}, NaN, 0");

            errorLogger = new LoggerConfiguration().ConfigureSinks(targets, level, "latency-logger").CreateLogger();

        }

        public void Start()
        {
            var updateInterval = TimeSpan.FromMilliseconds(333d);

            var now = DateTime.UtcNow;
            var lastNow = now;
            TimeSpan lag = TimeSpan.Zero;

            while (true)
            {

                now = DateTime.UtcNow;
                var elapsed = now - lastNow;

                lastNow = now;
                lag += elapsed;
                while (lag >= updateInterval)
                {
                    var timeoutSource = new CancellationTokenSource(updateInterval/2);
                    Parallel.ForEach(assignedTopicPartitionOffsets, tpo =>
                    {
                        TimeSpan latency = TimeSpan.FromMilliseconds(-1);
                        try
                        {
                            var consumeRes = consumer.Consume(timeoutSource.Token);
                            var output = consumeRes.Message.Value.Split("$");
                            var inputTime = DateTime.ParseExact(output[0], "yyyyMMddHHmmssFFFFF", null, DateTimeStyles.None);
                            var outputTime = consumeRes.Message.Timestamp.UtcDateTime; // DateTime.ParseExact(output[1], "yyyyMMddHHmmssFFFFF", null, DateTimeStyles.None);
                            latency = outputTime - inputTime;
                            consumer.Seek(new TopicPartitionOffset(tpo.TopicPartition, Offset.End));
                        }
                        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested) { } //shh
                        catch (Exception e) //log other exception types without stopping..
                        {
                            errorLogger.Warning(e, "Error while consuming from kafka topic");
                        }
                        latencyLogger.Information($"{now:hh:mm:ss:ffffff}, {(latency.TotalMilliseconds > 0 ? (object)latency.TotalMilliseconds : "NaN")}, {tpo.Partition.Value}");
                    });
                    lag -= updateInterval;
                }
                /*
                Parallel.ForEach(assignedTopicPartitionOffsets, tpo =>
                {
                    try
                    {
                        //consumer.Seek(new TopicPartitionOffset(tpo.TopicPartition, consumer.Position(tpo.TopicPartition) - 1));
                    } 
                    catch(Exception e)
                    {
                        errorLogger.Warning(e, "Error while seeking kafka topic");

                    }
                });
                */
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
            assignedTopicPartitionOffsets = topicPartitions;
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
