using Autofac;
using AutofacSerilogIntegration;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Models;
using Microsoft.Azure.Storage;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Logging
{   
    public static class AutofacSerilogExtensions
    {
        internal static ContainerBuilder UseDefaultLogger(this ContainerBuilder builder, ILogConfiguration config, string instanceName)
        {
            _ = config ?? throw new ArgumentNullException(nameof(config));

            var targetFlags = config.TargetFlags;
            var logLevel = config.EventLevel;

            var loggerConfig = new LoggerConfiguration().ConfigureSinks(targetFlags, logLevel, instanceName);
            var log = loggerConfig.CreateLogger();
            builder.RegisterLogger(log, autowireProperties: true);
            return builder;
        }

        public static ContainerBuilder UseLogging(this ContainerBuilder builder, ILogConfiguration config, string instanceName)
        {
            builder.RegisterInstance(config).AsImplementedInterfaces();
            builder.UseDefaultLogger(config, instanceName);
            builder.RegisterType<MetricLogger>().AsImplementedInterfaces().SingleInstance();
            return builder;
        }
    }
}
