using BlackSP.Checkpointing;
using BlackSP.Infrastructure;
using BlackSP.Infrastructure.Builders;
using BlackSP.Infrastructure.Builders.Graph;
using BlackSP.Infrastructure.Models;
using Serilog.Events;
using System;
using System.Threading.Tasks;

namespace BlackSP.StreamBench
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var useSimulator = true;
            var workload = Workload.Identity;

            var logTargets = LogTargetFlags.Console | (useSimulator ? LogTargetFlags.File : LogTargetFlags.AzureBlob);
            var logLevel = LogEventLevel.Information;

            var appBuilder = useSimulator ? Simulator.Hosting.CreateDefaultApplicationBuilder() : CRA.Hosting.CreateDefaultApplicationBuilder();
            var app = await appBuilder
                .ConfigureLogging(new LogConfiguration(logTargets, logLevel))
                .ConfigureCheckpointing(new CheckpointConfiguration(CheckpointCoordinationMode.Uncoordinated, true, 45))
                .ConfigureOperators((builder) => ConfigureGraph(workload, builder))
                .Build();

            await app.RunAsync();
        }

        static void ConfigureGraph(Workload workload, IVertexGraphBuilder graphBuilder)
        {
            switch (workload)
            {
                case Workload.Identity: 
                    WorkloadConfiguration.ConfigureIdentity(graphBuilder);
                    break;
                case Workload.Sample: 
                    //TODO
                    break;
                case Workload.Projection:
                    //TODO
                    break;
                case Workload.Grep:
                    //TODO
                    break;
                case Workload.WordCount:
                    WorkloadConfiguration.ConfigureWordCount(graphBuilder);
                    break;
                case Workload.DistinctCount:
                    //TODO
                    break;
                case Workload.Statistics:
                    //TODO
                    break;
            }
        }
    }
}
