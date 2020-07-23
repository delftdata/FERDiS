using Autofac;
using AutofacSerilogIntegration;
using BlackSP.Kernel.Models;
using Microsoft.Azure.Storage;
using Serilog;
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
        public static ContainerBuilder UseSerilog(this ContainerBuilder builder, LogEventLevel logLevel, LogTargetFlags targetFlags)
        {

            builder.Register<ILogger>((c, p) =>
            {
                
                var vertexConfig = c.Resolve<IVertexConfiguration>();
                var logConfig = new LoggerConfiguration();
                if(targetFlags.HasFlag(LogTargetFlags.Console))
                {
                    logConfig.WriteTo.Console(logLevel);
                }
                if (targetFlags.HasFlag(LogTargetFlags.File))
                {
                    logConfig.WriteTo.RollingFile(AppDomain.CurrentDomain.BaseDirectory + $"logs/{vertexConfig.InstanceName}/{{Date}}.log", logLevel);
                }
                if (targetFlags.HasFlag(LogTargetFlags.AzureBlob))
                {
                    var connectionString = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONN_STRING"));
                    logConfig.WriteTo.AzureBlobStorage(connectionString, logLevel, "logs", $"{vertexConfig.InstanceName}/{{yyyy}}-{{MM}}-{{dd}}.log");
                }
                return logConfig.CreateLogger();
            }).InstancePerLifetimeScope();

            builder.RegisterLogger()//HMMM...

            return builder;
        }
    }
}
