using BlackSP.Benchmarks.Kafka;
using BlackSP.Kernel.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.MetricCollection
{

    /// <summary>
    /// Consumes the output topic (containing input & output timestamps)
    /// </summary>
    public class ThroughputCalculatingConsumer : KafkaConsumerBase<string>
    {

        public ThroughputCalculatingConsumer(IVertexConfiguration vertexConfig, ILogger logger): base(vertexConfig, logger)
        {

        }

        protected override string TopicName => "output";


        public void Start()
        {

        }

    }
}
