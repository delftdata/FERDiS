using BlackSP.Benchmarks.PageRank.Events;
using BlackSP.Benchmarks.PageRank.Models;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace BlackSP.Benchmarks.PageRank.Operators
{
    public class AdjacencySourceOperator : Kafka.KafkaConsumerBase<Adjacency>, ISourceOperator<AdjacencyEvent>
    {
        protected override string TopicName => Adjacency.KafkaTopicName;


        public AdjacencySourceOperator(IVertexConfiguration vertexConfig, ILogger logger): base(vertexConfig, logger)
        {

        }

        public AdjacencyEvent ProduceNext(CancellationToken t)
        {
            var consumeResult = Consumer.Consume(t);
            if (consumeResult.IsPartitionEOF)
            {
                throw new InvalidOperationException($"Unexpected EOF while consuming kafka topic {TopicName} on partition {consumeResult.Partition}");
            }
            //ensure local offset is stored before returning msg
            UpdateOffsets(consumeResult.Partition, (int)consumeResult.Offset);
            var adjacency = consumeResult.Message.Value ?? throw new InvalidDataException("Received null Auction object from Kafka");
            return new AdjacencyEvent
            {
                Key = adjacency.PageId.ToString(),
                Adjacancy = adjacency
            };
        }
    }
}
