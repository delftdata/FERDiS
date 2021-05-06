using BlackSP.Benchmarks.Kafka;
using BlackSP.Kernel.Configuration;
using Confluent.Kafka;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Logging;
using Serilog.Events;

namespace BlackSP.Benchmarks.MetricCollection
{

    /// <summary>
    /// Consumes the output topic (containing input & output timestamps)
    /// </summary>
    public class E2ELatencyCalculatingConsumer
    {

        private readonly IConsumer<int, string> consumer;
        private readonly ILogger latencyLogger;
        
        public E2ELatencyCalculatingConsumer()
        {
            consumer = InitKafka();
            var targets = (LogTargetFlags)int.Parse(Environment.GetEnvironmentVariable("LOG_TARGET_FLAGS"));
            var level = (LogEventLevel)int.Parse(Environment.GetEnvironmentVariable("LOG_EVENT_LEVEL"));
            latencyLogger = new LoggerConfiguration().ConfigureMetricSinks(targets, level, "latency", "performance").CreateLogger();
            latencyLogger.Information("timestamp, latency_ms");
        }

        public IConsumer<int, string> InitKafka()
        {
            var builder = new ConsumerBuilder<int, string>(new ConsumerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                GroupId = "e2e-latency",
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest
            });
            
            return builder.Build();
        }

        public void Start()
        {
            var consumeRes = consumer.Consume();
            var outputTime = consumeRes.Message.Timestamp.UtcDateTime;
            var inputTime = DateTime.Parse(consumeRes.Message.Value);
            var latency = outputTime - inputTime;
            latencyLogger.Information($"{outputTime:hh:mm:ss:ffffff}, {(int)latency.TotalMilliseconds}");
        }

    }
}
