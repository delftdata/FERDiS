using Autofac;
using AutofacSerilogIntegration;
using BlackSP.Kernel.Logging;
using BlackSP.Kernel.Models;
using Microsoft.Azure.Storage;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Extensions
{   
    public static class AutofacSerilogExtensions
    {
        public static ContainerBuilder UseSerilog(this ContainerBuilder builder, ILogConfiguration config, string instanceName)
        {
            var targetFlags = config.TargetFlags;
            var logLevel = config.EventLevel;
            
            var logConfig = new LoggerConfiguration().MinimumLevel.Verbose();
            
            if (targetFlags.HasFlag(LogTargetFlags.Console))
            {
                logConfig.WriteTo.Console(logLevel, 
                    outputTemplate: $"[{{Timestamp:HH:mm:ss}} {{Level:u3}}] [{instanceName}] {{Message}}{{NewLine}}{{Exception}}",
                    theme: AnsiConsoleTheme.Literate);
            }
            if (targetFlags.HasFlag(LogTargetFlags.File))
            {
                logConfig.WriteTo.RollingFile(AppDomain.CurrentDomain.BaseDirectory + $"logs/{instanceName}/{{Date}}.log", logLevel);
            }
            if (targetFlags.HasFlag(LogTargetFlags.AzureBlob))
            {
                var connectionString = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONN_STRING"));
                logConfig.WriteTo.AzureBlobStorage(connectionString, logLevel, "logs", $"{instanceName}/{{yyyy}}-{{MM}}-{{dd}}.log");
            }
            var log = logConfig.CreateLogger();

            builder.RegisterLogger(log, autowireProperties: true);

            return builder;
        }
    }
}
