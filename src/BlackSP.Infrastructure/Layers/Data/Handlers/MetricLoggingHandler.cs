using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Logging;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        private Timer _timer;
        private object _metricWindowLock;
        public MetricLoggingHandler(IMetricLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _latencyMillis = new List<int>();
            _metricWindowSize = TimeSpan.FromMilliseconds(Constants.MetricLoggingIntervalMs);

            _metricWindowLock = new object();
            _timer = new Timer(LogPerformanceMetrics, null, _metricWindowSize, _metricWindowSize);
            ResetWindow();
        }

        private void LogPerformanceMetrics(object state)
        {
            lock (_metricWindowLock)
            {
                var throughput = (int)(_eventCountInWindow / _metricWindowSize.TotalSeconds);
                var latencyMin = 0; 
                var latencyMax = 0; 
                var latencyAvg = 0; 
                if(_latencyMillis.Any())
                {
                    latencyMin = _latencyMillis.Min();
                    latencyMax = _latencyMillis.Max();
                    latencyAvg = (int)_latencyMillis.Average();
                }
                _logger.Performance(throughput, latencyMin, latencyAvg, latencyMax);
                ResetWindow();
            }
        }

        private void ResetWindow()
        {
            _metricWindowStart = DateTime.UtcNow;
            _latencyMillis = new List<int>();
            _eventCountInWindow = 0;
        }

        protected override Task<IEnumerable<TMessage>> Handle(EventPayload payload, CancellationToken t)
        {
            lock(_metricWindowLock)
            {
                _eventCountInWindow += payload.Event.EventCount();
                var latencyMs = (int)(DateTime.UtcNow - AssociatedMessage.CreatedAtUtc).TotalMilliseconds;
                _latencyMillis.Add(latencyMs);

                AssociatedMessage.AddPayload(payload); //re-add the payload 
                return Task.FromResult(AssociatedMessage.Yield());
            }
        }
    }
}
