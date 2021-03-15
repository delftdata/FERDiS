using BlackSP.Core.Extensions;
using BlackSP.Kernel.Extensions;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.Handlers
{

    /// <summary>
    /// Simple generic message handler that calculates metrics like latency and throughput and logs these
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public class MetricLoggingHandler<TMessage> : IHandler<TMessage>
        where TMessage : IMessage
    {

        private readonly ILogger _logger;

        private TimeSpan _metricWindowSize;
        private DateTime _metricWindowStart;

        private int _eventCountInWindow;
        private List<int> _latencyMillis;

        public MetricLoggingHandler(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _latencyMillis = new List<int>();
            _metricWindowSize = TimeSpan.FromSeconds(Constants.MetricIntervalSeconds);
        }

        public Task<IEnumerable<TMessage>> Handle(TMessage message)
        {
            var now = DateTime.UtcNow;
            if (_metricWindowStart != default && _metricWindowStart + _metricWindowSize < now) //window closes
            {
                var throughput = (int)(_eventCountInWindow / _metricWindowSize.TotalSeconds);
                var latencyMin = _latencyMillis.Min();
                var latencyMax = _latencyMillis.Max();
                var latencyAvg = (int)_latencyMillis.Average();
                _logger.Information($"Metrics [from:{_metricWindowStart:HH:mm:ss:fff}] [to:{now:HH:mm:ss:fff}] [tp:{throughput}] [lat-min:{latencyMin}] [lat-avg:{latencyAvg}] [lat-max:{latencyMax}]");
                _metricWindowStart = default;
            }

            if (_metricWindowStart == default) //new window
            {
                _metricWindowStart = DateTime.UtcNow;
                _latencyMillis = new List<int>();
                _eventCountInWindow = 0;
            }

            _eventCountInWindow++;
            var latencyMs = (int)(now - message.CreatedAtUtc).TotalMilliseconds;
            _latencyMillis.Add(latencyMs);
            
            return Task.FromResult(message.Yield());
        }
    }
}
