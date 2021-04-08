using BlackSP.Core;
using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Logging;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{

    /// <summary>
    /// Simple generic message handler that calculates metrics like latency and throughput and logs these
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public class MetricLoggingHandler<TMessage> : ForwardingPayloadHandlerBase<TMessage, EventPayload>
        where TMessage : IMessage
    {
        private readonly IMetricLogger _logger;

        private TimeSpan _metricWindowSize;
        private DateTime _metricWindowStart;

        private int _eventCountInWindow;
        private List<int> _latencyMillis;


        public MetricLoggingHandler(IMetricLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _latencyMillis = new List<int>();
            _metricWindowSize = TimeSpan.FromSeconds(Constants.MetricIntervalSeconds);

        }

        protected override Task<IEnumerable<TMessage>> Handle(EventPayload payload)
        {
            var now = DateTime.UtcNow;
            if (_metricWindowStart != default && _metricWindowStart + _metricWindowSize < now) //window closes
            {
                var throughput = (int)(_eventCountInWindow / _metricWindowSize.TotalSeconds);
                var latencyMin = _latencyMillis.Min();
                var latencyMax = _latencyMillis.Max();
                var latencyAvg = (int)_latencyMillis.Average();
                _logger.Performance(throughput, latencyMin, latencyAvg, latencyMax);
                _metricWindowStart = default;
            }

            if (_metricWindowStart == default) //new window
            {
                _metricWindowStart = DateTime.UtcNow;
                _latencyMillis = new List<int>();
                _eventCountInWindow = 0;
            }

            _eventCountInWindow += payload.Event.EventCount();
            var latencyMs = (int)(now - AssociatedMessage.CreatedAtUtc).TotalMilliseconds;
            _latencyMillis.Add(latencyMs);

            AssociatedMessage.AddPayload(payload); //re-add the payload 
            return Task.FromResult(AssociatedMessage.Yield());
        }
    }
}
