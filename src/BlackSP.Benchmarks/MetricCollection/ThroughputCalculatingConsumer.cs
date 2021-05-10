using BlackSP.Benchmarks.Kafka;
using BlackSP.Kernel.Configuration;
using BlackSP.Logging;
using Confluent.Kafka;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.MetricCollection
{

    /// <summary>
    /// Consumes the output topic (containing input & output timestamps)
    /// </summary>
    public class ThroughputCalculatingConsumer
    {

        private readonly IConsumer<int, string> consumer;
        private readonly ILogger throughputLogger;
        private readonly ILogger errorLogger;



        private IEnumerable<TopicPartitionOffset> assignedTopicPartitionOffsets;

        public ThroughputCalculatingConsumer()
        {
            consumer = InitKafka();
            var targets = (LogTargetFlags)int.Parse(Environment.GetEnvironmentVariable("LOG_TARGET_FLAGS"));
            var level = (LogEventLevel)int.Parse(Environment.GetEnvironmentVariable("LOG_EVENT_LEVEL"));
            throughputLogger = new LoggerConfiguration().ConfigureMetricSinks(targets, level, "throughput", "performance").CreateLogger();
            throughputLogger.Information("timestamp, throughput");

            errorLogger = new LoggerConfiguration().ConfigureSinks(targets, level, "throughput-logger").CreateLogger();

        }



        public void Start()
        {
            var now = DateTime.UtcNow;
            var lastNow = now;
            var lastWrite = now;

            TimeSpan lag = TimeSpan.Zero;
            TimeSpan updateInterval = TimeSpan.FromMilliseconds(333d);

            var lastWmOffsets = new Dictionary<Partition, Offset>();
            foreach (var tpo in assignedTopicPartitionOffsets)
            {
                lastWmOffsets.Add(tpo.Partition, 0l);
            }

            object lockObj = new object();

            Task.Run(() =>
            {
                while(true)
                {
                    consumer.Consume(); //consume as fast as possible to keep watermarks up to date
                }
            });


            while (true)
            {

                now = DateTime.UtcNow;
                var elapsed = now - lastNow;

                lastNow = now;
                lag += elapsed;
                while(lag >= updateInterval)
                {
                    long totalNew = 0l;
                    var printStamp = DateTime.Now;

                    var printDelta = printStamp - lastWrite;
                    if(printDelta > TimeSpan.Zero)
                    {
                        Parallel.ForEach(assignedTopicPartitionOffsets, tpo =>
                        {
                            var wmOffsets = consumer.GetWatermarkOffsets(tpo.TopicPartition);
                            var high = wmOffsets.High == Offset.Unset ? 0 : wmOffsets.High;

                            var delta = high - lastWmOffsets[tpo.Partition];
                            lock (lockObj) //not best performing solution but its good enough
                            {
                                totalNew += delta;
                            }
                            lastWmOffsets[tpo.Partition] = high;
                        });
                        throughputLogger.Information($"{printStamp:hh:mm:ss:ffffff}, {(int)(totalNew/printDelta.TotalSeconds)}");
                        lastWrite = printStamp;
                    } 
                    else
                    {
                        Console.WriteLine("huh");
                    }

                    //Console.WriteLine("Lag was " + lag);
                    lag -= updateInterval;
                    //Console.WriteLine("Lag is " + lag);

                }
            }
        }

        private IConsumer<int, string> InitKafka()
        {
            var builder = new ConsumerBuilder<int, string>(new ConsumerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                GroupId = "throughput",
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest
            }).SetErrorHandler((c,e) => { errorLogger.Warning($"Kafka error: {e}"); });

            var consumer = builder.Build();
            var tpos = GetAssignedTopicPartitions("output");
            assignedTopicPartitionOffsets = tpos;
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
