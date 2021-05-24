using BlackSP.Benchmarks.Kafka;
using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Benchmarks.NEXMark.Models;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using Confluent.Kafka;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.NEXMark.Operators.LocalItem
{
    public class AuctionPersonSinkOperator : ISinkOperator<AuctionPersonEvent>
    {

        private readonly ILogger _logger;
        private IProducer<int, string> producer;


        public AuctionPersonSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var config = new ProducerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                Partitioner = Partitioner.Consistent
            };
            producer = new ProducerBuilder<int, string>(config).SetErrorHandler((prod, err) => logger.Warning($"Output produce error: {err}")).Build();
        }

        public async Task Sink(AuctionPersonEvent @event)
        {
            var auction = @event.Auction;
            var person = @event.Person;
            //_logger.Information($"[ {person.FullName}, {person.Address.Street}, {person.Address.Zipcode}, {person.Address.Province}, {auction.CategoryId} ]");

            var outputValue = $"{@event.EventTime:yyyyMMddHHmmssFFFFF}${DateTime.UtcNow:yyyyMMddHHmmssFFFFF}${((IEvent)@event).EventCount()}";
            producer.ProduceAsync("output", new Message<int, string> { Key = @event.Key ?? default, Value = outputValue });
        }
    }
}
