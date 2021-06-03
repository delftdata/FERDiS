using BlackSP.Checkpointing;
using BlackSP.Kernel.Operators;
using BlackSP.Benchmarks.WordCount.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using BlackSP.Benchmarks.Kafka;

namespace BlackSP.Benchmarks.WordCount.Operators
{
    class KafkaWordCountSink : ISinkOperator<WordEvent>, IDisposable
    {

        private readonly ILogger _logger;

        [ApplicationState]
        private IDictionary<string, int> _wordCountMap;

        private IProducer<int, string> producer;
        private bool disposedValue;

        public KafkaWordCountSink(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _wordCountMap = new Dictionary<string, int>();

            var config = new ProducerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                Partitioner = Partitioner.Random,
                LingerMs = 100,//high linger can improve throughput
            };
            producer = new ProducerBuilder<int, string>(config).SetErrorHandler((prod, err) => logger.Warning($"Output produce error: {err}")).Build();
        }

        public Task Sink(WordEvent @event)
        {
            if(_wordCountMap.ContainsKey(@event.Word))
            {
                _wordCountMap[@event.Word] += @event.Count;
            } 
            else
            {
                _wordCountMap.Add(@event.Word, @event.Count);
            }
            //var wordCountStrings = _wordCountMap.OrderBy(p => p.Key).Select(x => x.Key + "=" + x.Value).ToArray();
            //_logger.Debug($"WordCount: {string.Join("; ", wordCountStrings)}");
            
            var outputValue = @event.EventTime.ToString("yyyyMMddHHmmssFFFFF")+"$"+DateTime.UtcNow.ToString("yyyyMMddHHmmssFFFFF")+"$"+@event.EventCount();
            producer.Produce("output", new Message<int, string> { Key = @event.Key ?? default, Value = outputValue });
            return Task.CompletedTask;
            //producer.Flush();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    producer.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~WordCountLoggerSink()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
