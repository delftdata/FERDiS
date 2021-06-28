using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Logging;
using Microsoft.Azure.Storage;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;

namespace BlackSP.Logging
{
    public class MetricLogger : IMetricLogger
    {

        private readonly ILogConfiguration _config;
        private readonly string _instanceName;

        private ILogger _defaultLogger;
        private ILogger _checkpointLogger;
        private ILogger _recoveryLogger;
        private ILogger _lostMessagesLogger;


        public MetricLogger(IVertexConfiguration vertexConfig, ILogConfiguration logConfig, ILogger defaultLogger)
        {
            _instanceName = vertexConfig?.InstanceName ?? throw new ArgumentNullException(nameof(vertexConfig));
            _config = logConfig ?? throw new ArgumentNullException(nameof(logConfig));
            _defaultLogger = defaultLogger ?? throw new ArgumentNullException(nameof(defaultLogger));
            
            InitialiseLoggers();
        }

        public ILogger GetDefaultLogger() => _defaultLogger;

        public void Checkpoint(long bytes, TimeSpan time, bool wasForced)
        {
            _checkpointLogger.Information($"{DateTime.UtcNow:hh:mm:ss:ffffff}, {wasForced}, {(int)time.TotalMilliseconds}, {bytes}");
        }


        public void Recovery(TimeSpan time, TimeSpan distance)
        {
            _recoveryLogger.Information($"{DateTime.UtcNow:hh:mm:ss:ffffff}, {(int)time.TotalMilliseconds}, {(int)distance.TotalMilliseconds}");
        }

        public void LostMessages(int messageCount, string downstreamInstanceName)
        {
            _lostMessagesLogger.Information($"{DateTime.UtcNow:hh:mm:ss:ffffff}, {messageCount.ToString()}, {downstreamInstanceName}");
        }

        /// <summary>
        /// Init metric logger objects and write header line
        /// </summary>
        private void InitialiseLoggers()
        {
            _checkpointLogger = new LoggerConfiguration().ConfigureMetricSinks(_config.TargetFlags, _config.EventLevel, _instanceName, "checkpoint").CreateLogger();
            _checkpointLogger.Information("timestamp, forced, taken_ms, bytes");
            _recoveryLogger = new LoggerConfiguration().ConfigureMetricSinks(_config.TargetFlags, _config.EventLevel, _instanceName, "recovery").CreateLogger();
            _recoveryLogger.Information("timestamp, restored_ms, rollback_ms");
            _lostMessagesLogger = new LoggerConfiguration().ConfigureMetricSinks(_config.TargetFlags, _config.EventLevel, _instanceName, "lost-messages").CreateLogger();
            _lostMessagesLogger.Information("timestamp, message_count, instance_name");
        }

    }

    
}
