using BlackSP.Benchmarks.Kafka;
using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using Confluent.Kafka;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.NEXMark.Operators.Projection
{
    class BidSinkOperator : ISinkOperator<BidEvent>
    {

        private readonly ILogger _logger;
        private IProducer<int, string> producer;

        public BidSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var config = new ProducerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                Partitioner = Partitioner.Consistent
            };
            producer = new ProducerBuilder<int, string>(config).SetErrorHandler((prod, err) => logger.Warning($"Output produce error: {err}")).Build();
        }

        public Task Sink(BidEvent @event)
        {
            var bid = @event.Bid;
            //_logger.Information($"pid:{bid.PersonId},aid{bid.AuctionId},amt:{bid.Amount}");

            var outputValue = $"{@event.EventTime:yyyyMMddHHmmssFFFFF}${DateTime.UtcNow:yyyyMMddHHmmssFFFFF}${((IEvent)@event).EventCount()}";
            producer.ProduceAsync("output", new Message<int, string> { Key = @event.Key ?? default, Value = outputValue });
            return Task.CompletedTask;
        }
    }
}
