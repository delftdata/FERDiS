using BlackSP.Kernel.Configuration;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Logging
{
    public static class SerilogConfigurationExtensions
    {

        public static LoggerConfiguration ConfigureSinks(this LoggerConfiguration loggerConfig, LogTargetFlags targetFlags, LogEventLevel logLevel, string instanceName)
        {
            var logConfig = loggerConfig.MinimumLevel.Verbose();
            if (targetFlags.HasFlag(LogTargetFlags.Console))
            {
                logConfig.WriteTo.Console(logLevel,
                    outputTemplate: $"[{{Timestamp:hh:mm:ss:ffffff}}] [{instanceName} {{Level:u3}}] {{Message}}{{NewLine}}{{Exception}}",
                    theme: AnsiConsoleTheme.Literate);
            }
            if (targetFlags.HasFlag(LogTargetFlags.File))
            {
                logConfig.WriteTo.RollingFile($"{AppDomain.CurrentDomain.BaseDirectory}logs/{instanceName}-{{Date}}.log", logLevel,
                    outputTemplate: $"[{{Timestamp:hh:mm:ss:ffffff}}] [{instanceName} {{Level:u3}}] {{Message}}{{NewLine}}{{Exception}}");
            }
            if (targetFlags.HasFlag(LogTargetFlags.AzureBlob))
            {
                var connectionString = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"));
                logConfig.WriteTo.AzureBlobStorage(connectionString, logLevel, "logs", $"{instanceName}-{{yyyy}}-{{MM}}-{{dd}}.log",
                    outputTemplate: $"[{{Timestamp:hh:mm:ss:ffffff}}] [{instanceName} {{Level:u3}}] {{Message}}{{NewLine}}{{Exception}}");
            }

            return loggerConfig;
        }

        /// <summary>
        /// Configures sinks with a subfolder and custom format ideal for logging CSV style data
        /// </summary>
        /// <param name="loggerConfig"></param>
        /// <param name="targetFlags"></param>
        /// <param name="logLevel"></param>
        /// <param name="instanceName"></param>
        /// <param name="subFolder"></param>
        /// <returns></returns>
        public static LoggerConfiguration ConfigureMetricSinks(this LoggerConfiguration loggerConfig, LogTargetFlags targetFlags, LogEventLevel logLevel, string instanceName, string subFolder)
        {
            var logConfig = loggerConfig.MinimumLevel.Verbose();
            if (targetFlags.HasFlag(LogTargetFlags.Console))
            {
                // never log metrics console
                //logConfig.WriteTo.Console(logLevel, outputTemplate: $"[{instanceName}] {{Message}}{{NewLine}}{{Exception}}", theme: AnsiConsoleTheme.Literate); //log usual format to console anyway
            }
            if (targetFlags.HasFlag(LogTargetFlags.File))
            {
                logConfig.WriteTo.RollingFile($"{AppDomain.CurrentDomain.BaseDirectory}logs/{subFolder}/{instanceName}-{{Date}}.log", logLevel,
                    outputTemplate: $"{{Message}}{{NewLine}}");
            }
            if (targetFlags.HasFlag(LogTargetFlags.AzureBlob))
            {
                var connectionString = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"));
                
                logConfig.WriteTo.AzureBlobStorage(connectionString, logLevel, "logs", $"{subFolder}/{instanceName}-{{yyyy}}-{{MM}}-{{dd}}.log",
                    outputTemplate: $"{{Message}}{{NewLine}}", writeInBatches: true);
            }

            return loggerConfig;
        }
    }
}
