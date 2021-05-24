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

namespace BlackSP.Benchmarks.NEXMark.Operators.AverageSellingPriceBySeller
{
    public class AveragePriceSinkOperator : ISinkOperator<AveragePricePersonEvent>
    {

        private readonly ILogger _logger;
        private IProducer<int, string> producer;

        public AveragePriceSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var config = new ProducerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                Partitioner = Partitioner.Consistent
            };
            producer = new ProducerBuilder<int, string>(config).SetErrorHandler((prod, err) => logger.Warning($"Output produce error: {err}")).Build();
        }

        public Task Sink(AveragePricePersonEvent @event)
        {
            //_logger.Information($"Person {@event.PersonId:0000} avg price {@event.AverageSellingPrice:N2}");
            var outputValue = $"{@event.EventTime:yyyyMMddHHmmssFFFFF}${DateTime.UtcNow:yyyyMMddHHmmssFFFFF}${((IEvent)@event).EventCount()}";
            producer.ProduceAsync("output", new Message<int, string> { Key = @event.Key ?? default, Value = outputValue });
            return Task.CompletedTask;
        }
    }
}
