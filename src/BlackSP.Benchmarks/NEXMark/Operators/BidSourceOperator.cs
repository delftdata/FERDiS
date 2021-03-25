using BlackSP.Benchmarks.Kafka;
using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Benchmarks.NEXMark.Models;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.IO;
using System.Threading;

namespace BlackSP.Benchmarks.NEXMark.Operators
{
    public class BidSourceOperator : KafkaConsumerBase<Bid>, ISourceOperator<BidEvent>
    {
        protected override string TopicName => Bid.KafkaTopicName;

        public BidSourceOperator(IVertexConfiguration vertexConfig, ILogger logger) : base(vertexConfig, logger)
        {
        }

        public BidEvent ProduceNext(CancellationToken t)
        {
            var consumeResult = Consumer.Consume(t);
            if(consumeResult.IsPartitionEOF)
            {
                throw new InvalidOperationException($"Unexpected EOF while consuming kafka topic {TopicName} on partition {consumeResult.Partition}");
            }
            //ensure local offset is stored before returning msg
            UpdateOffsets(consumeResult.Partition, (int)consumeResult.Offset);
            var bid = consumeResult.Message.Value ?? throw new InvalidDataException("Received null Bid object from Kafka");
            //Logger.Warning($"Received bid: {bid.PersonId} offered {bid.Amount} for {bid.AuctionId}");
            return new BidEvent { 
                Key = bid.AuctionId.ToString(), 
                Bid = bid, 
                EventTime = DateTime.Now };
        }

        
    }
}
