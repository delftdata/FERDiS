using BlackSP.Checkpointing;
using BlackSP.Infrastructure;
using BlackSP.Infrastructure.Builders;
using BlackSP.Infrastructure.Models;
using BlackSP.ThroughputExperiment.Events;
using Serilog.Events;
using System.Threading.Tasks;

namespace BlackSP.ThroughputExperiment
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var useSimulator = true;

            //Worker.Launch("crainst01", 1500, new AzureDataProvider(), null);
            var logTargets = LogTargetFlags.Console;
            logTargets = logTargets | (useSimulator ? LogTargetFlags.File : LogTargetFlags.AzureBlob);
            
            var logLevel = LogEventLevel.Information;

            var appBuilder = useSimulator ? Simulator.Hosting.CreateDefaultApplicationBuilder() : CRA.Hosting.CreateDefaultApplicationBuilder();
            var app = await appBuilder
                .ConfigureLogging(new LogConfiguration(logTargets, logLevel))
                .ConfigureCheckpointing(new CheckpointConfiguration(CheckpointCoordinationMode.Coordinated, false, 10))
                .ConfigureOperators(ConfigureOperatorGraph)
                .Build();

            await app.RunAsync();
        }

        static void ConfigureOperatorGraph(IVertexGraphBuilder graph)
        {
            var source = graph.AddSource<SampleSourceOperator, SampleEvent>(1);
            var sink = graph.AddSink<SampleSinkOperator, SampleEvent>(1);

            source.Append(sink);
        }
    }
}
