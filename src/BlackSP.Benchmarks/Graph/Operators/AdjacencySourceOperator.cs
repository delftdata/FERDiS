using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Benchmarks.Graph.Models;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.Graph.Operators
{
    public class AdjacencySourceOperator : Kafka.KafkaConsumerBase<Adjacency>, ISourceOperator<AdjacencyEvent>
    {
        protected override string TopicName => Adjacency.KafkaTopicName;


        public AdjacencySourceOperator(IVertexConfiguration vertexConfig, ILogger logger): base(vertexConfig, logger)
        {

        }

        public AdjacencyEvent ProduceNext(CancellationToken t)
        {
            Task.Delay(1).Wait(); //TODO: remove nasty throttle
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
                Key = adjacency.PageId,
                Adjacancy = adjacency,
                EventTime = DateTime.Now
            };
        }
    }
}
