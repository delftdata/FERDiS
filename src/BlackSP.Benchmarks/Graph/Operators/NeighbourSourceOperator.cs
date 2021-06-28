using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Benchmarks.Graph.Models;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace BlackSP.Benchmarks.Graph.Operators
{
    public class NeighbourSourceOperator : Kafka.KafkaSourceConsumerBase<Neighbour>, ISourceOperator<HopEvent>
    {
        protected override string TopicName => Neighbour.KafkaTopicName;

        public NeighbourSourceOperator(IVertexConfiguration vertexConfig, ILogger logger) : base(vertexConfig, logger)
        {

        }

        public HopEvent ProduceNext(CancellationToken t)
        {
            var consumeResult = Consumer.Consume(t);
            if (consumeResult.IsPartitionEOF)
            {
                throw new InvalidOperationException($"Unexpected EOF while consuming kafka topic {TopicName} on partition {consumeResult.Partition}");
            }
            //ensure local offset is stored before returning msg
            UpdateOffsets(consumeResult.Partition, (int)consumeResult.Offset);
            var nb = consumeResult.Message.Value ?? throw new InvalidDataException("Received null Auction object from Kafka");
            return new HopEvent
            {
                Key = nb.FromId,
                Neighbour = nb,
                EventTime = consumeResult.Message.Timestamp.UtcDateTime
            };
        }
    }
}
