using BlackSP.Checkpointing;
using BlackSP.Infrastructure;
using BlackSP.Infrastructure.Builders.Graph;
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
            
            var logLevel = LogEventLevel.Debug;

            var appBuilder = useSimulator ? Simulator.Hosting.CreateDefaultApplicationBuilder() : CRA.Hosting.CreateDefaultApplicationBuilder();
            var app = await appBuilder
                .ConfigureLogging(new LogConfiguration(logTargets, logLevel))
                .ConfigureCheckpointing(new CheckpointConfiguration(CheckpointCoordinationMode.Coordinated, false))
                .ConfigureOperators(ConfigureOperatorGraph)
                .Build();

            await app.RunAsync();
        }

        static void ConfigureOperatorGraph(IOperatorVertexGraphBuilder graph)
        {
            var source = graph.AddSource<SampleSourceOperator, SampleEvent>(1);
            var filter = graph.AddFilter<SampleFilterOperator, SampleEvent>(2);
            var mapper = graph.AddMap<SampleMapOperator, SampleEvent, SampleEvent>(2);
            //var aggregate = graph.AddAggregate<SampleAggregateOperator, SampleEvent, SampleEvent2>(2);
            var sink = graph.AddSink<SampleSinkOperator, SampleEvent>(1);

            source.Append(filter);
            filter.Append(mapper);
            //mapper.Append(aggregate);
            mapper.Append(sink);
            
        }
    }
}
