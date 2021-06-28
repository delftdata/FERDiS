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
using System.Linq;

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
            var fixedLag = TimeSpan.FromSeconds(5); //continuously lag behind by 5 seconds (we fetch specific offset at specific times instead of attempting real-time)
            
            var updateInterval = TimeSpan.FromMilliseconds(333d);
            

            var laggingNow = DateTime.UtcNow - fixedLag;
            var lastPrint = laggingNow;
            
            while (true)
            {
                laggingNow = DateTime.UtcNow - fixedLag;
                var elapsed = laggingNow - lastPrint;
                if(elapsed < updateInterval) //busy spin until next print moment is reached
                {
                    continue;
                }

                var timeoutSource = new CancellationTokenSource(10000); //TODO: consider increasing? may start lagging harder, not a real issue tho

                //fetch the next offsets that were delivered after the 'nextTs'
                var nextTs = new Timestamp(lastPrint, TimestampType.CreateTime);
                List<TopicPartitionOffset> tposForPrint;
                try
                {
                    tposForPrint = consumer.OffsetsForTimes(assignedTopicPartitionOffsets.Select(tpo => new TopicPartitionTimestamp(tpo.TopicPartition, nextTs)), TimeSpan.FromMilliseconds(10000));
                } catch(Exception e)
                {
                    errorLogger.Warning(e, "exception");
                    Thread.Sleep(1000);
                    continue;
                }
                foreach (var tpo in tposForPrint)
                {
                    TimeSpan latency = TimeSpan.FromMilliseconds(-1);
                    try
                    {
                        if(tpo.Offset != Offset.End) //no delivery after this offset, no need to calculate latency, its not there
                        {
                            consumer.Seek(tpo);// new TopicPartitionOffset(tpo.TopicPartition, tpo.Offset - 1));
                            var consumeRes = consumer.Consume(timeoutSource.Token);
                            var output = consumeRes.Message.Value.Split("$");
                            var inputTime = DateTime.ParseExact(output[0], "yyyyMMddHHmmssFFFFF", null, DateTimeStyles.None);
                            var outputTime = consumeRes.Message.Timestamp.UtcDateTime; // DateTime.ParseExact(output[1], "yyyyMMddHHmmssFFFFF", null, DateTimeStyles.None);
                            latency = outputTime - inputTime;
                        }                        
                    }
                    catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested) {
                    } //shh
                    catch (Exception e) //log other exception types without stopping..
                    {
                        errorLogger.Warning(e, "Error while consuming from kafka topic");
                    }
                    //Console.WriteLine($"{DateTime.UtcNow:hh:mm:ss:ffffff}");
                    latencyLogger.Information($"{lastPrint:hh:mm:ss:ffffff}, {(latency.TotalMilliseconds > 0 ? (object)(int)latency.TotalMilliseconds : "NaN")}, {tpo.Partition.Value}");
                }

                lastPrint += updateInterval;

            }

        }

        private IConsumer<int, string> InitKafka()
        {
            var builder = new ConsumerBuilder<int, string>(new ConsumerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                GroupId = "e2e-latency",
                EnableAutoCommit = true,
                AutoOffsetReset = AutoOffsetReset.Latest,
                MaxPartitionFetchBytes = 5000,
                FetchMaxBytes = 5000,
                MessageMaxBytes = 5000
            }).SetErrorHandler((c,e) => errorLogger.Warning($"Kafka error: {e}"));

            var consumer = builder.Build();
            var topicPartitions = GetAssignedTopicPartitions("output");
            assignedTopicPartitionOffsets = topicPartitions;
            consumer.Assign(topicPartitions);
            return consumer;
        }

        private IEnumerable<TopicPartitionOffset> GetAssignedTopicPartitions(string topicName)
        {
            var partitionCount = int.Parse(Environment.GetEnvironmentVariable("KAFKA_TOPIC_PARTITION_COUNT"));
            for (int i = 0; i < partitionCount; i++)
            {
                yield return new TopicPartitionOffset(new TopicPartition(topicName, i), Offset.End);
            }
        }

    }
}
