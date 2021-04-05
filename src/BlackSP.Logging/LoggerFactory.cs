using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Logging;
using Microsoft.Azure.Storage;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;

namespace BlackSP.Logging
{
    public class LoggerFactory : ILoggerFactory
    {

        private readonly ILogConfiguration _config;
        private readonly string _instanceName;

        private ILogger _defaultLogger;
        private ILogger _performanceLogger;
        private ILogger _checkpointLogger;
        private ILogger _recoveryLogger;


        public LoggerFactory(IVertexConfiguration vertexConfig, ILogConfiguration logConfig, ILogger defaultLogger)
        {
            _instanceName = vertexConfig?.InstanceName ?? throw new ArgumentNullException(nameof(vertexConfig));
            _config = logConfig ?? throw new ArgumentNullException(nameof(logConfig));
            _defaultLogger = defaultLogger ?? throw new ArgumentNullException(nameof(defaultLogger));
            
            InitialiseLoggers();
        }

        public ILogger GetDefaultLogger() => _defaultLogger;
        public ILogger GetPerformanceLogger() => _performanceLogger;
        public ILogger GetCheckpointLogger() => _checkpointLogger;
        public ILogger GetRecoveryLogger() => _recoveryLogger;

        private void InitialiseLoggers()
        {
            _performanceLogger = new LoggerConfiguration().ConfigureMetricSinks(_config.TargetFlags, _config.EventLevel, _instanceName, "performance").CreateLogger();
            _checkpointLogger = new LoggerConfiguration().ConfigureMetricSinks(_config.TargetFlags, _config.EventLevel, _instanceName, "checkpoint").CreateLogger();
            _recoveryLogger = new LoggerConfiguration().ConfigureMetricSinks(_config.TargetFlags, _config.EventLevel, _instanceName, "recovery").CreateLogger();
        }

    }

    
}
