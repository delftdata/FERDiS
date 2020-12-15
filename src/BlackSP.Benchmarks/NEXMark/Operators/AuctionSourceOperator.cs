using BlackSP.Benchmarks.Kafka;
using BlackSP.Benchmarks.NEXMark;
using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Benchmarks.NEXMark.Models;
using BlackSP.Checkpointing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BlackSP.Benchmarks.NEXMark.Operators
{
    public class AuctionSourceOperator : KafkaConsumerBase<Auction>, ISourceOperator<AuctionEvent>
    {
        protected override string TopicName => Auction.KafkaTopicName;

        public AuctionSourceOperator(IVertexConfiguration vertexConfig, ILogger logger) : base(vertexConfig, logger)
        {
        }

        public AuctionEvent ProduceNext(CancellationToken t)
        {
            var consumeResult = Consumer.Consume(t);
            if(consumeResult.IsPartitionEOF)
            {
                throw new InvalidOperationException($"Unexpected EOF while consuming kafka topic {TopicName} on partition {consumeResult.Partition}");
            }
            //ensure local offset is stored before returning msg
            UpdateOffsets(consumeResult.Partition, (int)consumeResult.Offset);
            var auction = consumeResult.Message.Value ?? throw new InvalidDataException("Received null Auction object from Kafka");
            return new AuctionEvent { 
                Key = auction.Id.ToString(), 
                Auction = auction, 
                EventTime = DateTime.Now 
            };
        }

        
    }
}
