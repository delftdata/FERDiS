using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Benchmarks.Kafka;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using Confluent.Kafka;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.Graph.Operators
{
    public class HopCountSinkOperator : ISinkOperator<HopEvent>
    {

        private readonly ILogger _logger;

        private readonly IDictionary<string, int> _results;
        private readonly IProducer<int, string> producer;
        public HopCountSinkOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _results = new Dictionary<string, int>();

            var config = new ProducerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                Partitioner = Partitioner.Random,
                LingerMs = 5,//high linger can improve throughput
            };
            producer = new ProducerBuilder<int, string>(config).SetErrorHandler((prod, err) => logger.Warning($"Output produce error: {err}")).Build();
        }

        public Task Sink(HopEvent @event)
        {
            var neighbour = @event.Neighbour;
            var key = $"{neighbour.FromId}-{neighbour.ToId}";
            if(!_results.ContainsKey(key))
            {
                _results[key] = int.MaxValue;
            } 
            if(neighbour.Hops < _results[key])
            {
                _results[key] = neighbour.Hops;

                if(Constants.LogNhopOutput)
                {
                    _logger.Information($"NHop: [{neighbour.FromId:D6}, {neighbour.ToId:D6}] = {neighbour.Hops}");
                }

            }
            var ievent = (IEvent)@event;
            var outputValue = @event.EventTime.ToString("yyyyMMddHHmmssFFFFF") + "$" + DateTime.UtcNow.ToString("yyyyMMddHHmmssFFFFF") + "$" + ievent.EventCount();
            producer.Produce("output", new Message<int, string> { Key = @event.Key ?? default, Value = outputValue });
            return Task.CompletedTask;
        }
    }
}
