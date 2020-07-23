using Autofac;
using AutofacSerilogIntegration;
using BlackSP.Kernel.Models;
using Microsoft.Azure.Storage;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Extensions
{   
    [Flags]
    public enum LogTargetFlags
    {
        None = 0,
        Console = 1 << 0,
        File = 1 << 1,
        AzureBlob = 1 << 2
    }

    public static class AutofacSerilogExtensions
    {
        public static ContainerBuilder UseSerilog(this ContainerBuilder builder, LogEventLevel logLevel, LogTargetFlags targetFlags, string instanceName)
        {
            var logConfig = new LoggerConfiguration().MinimumLevel.Verbose();
            
            if (targetFlags.HasFlag(LogTargetFlags.Console))
            {
                logConfig.WriteTo.Console(logLevel, outputTemplate: $"[{instanceName}] [{{Timestamp:HH:mm:ss}} {{Level:u3}}]  {{Message}}{{NewLine}}{{Exception}}");
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
